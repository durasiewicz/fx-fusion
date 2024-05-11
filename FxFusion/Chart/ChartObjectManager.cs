using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using FxFusion.Objects;

namespace FxFusion.Chart;

public class ChartObjectManager
{
    private record Command();

    private record DeleteObjectCommand(IChartObject ChartObject) : Command;

    private record AddHorizontalLineCommand(Point Position) : Command;

    private record AddHorizontalRayCommand(Point Position) : Command;

    private record MoveObjectCommand(IChartObject ChartObject, Point NewPosition) : Command;
    
    private readonly ConcurrentQueue<Command> _commands = new();
    private readonly List<IChartObject> _chartObjects = new();
    private Point? _pointerPosition;
    private IChartObject? _hoveredObject;
    private IChartObject? _selectedObject;
    private bool _pointerPressed;

    public void CreateHorizontalLine(Point position) => _commands.Enqueue(new AddHorizontalLineCommand(position));
    public void CreateHorizontalRay(Point position) => _commands.Enqueue(new AddHorizontalRayCommand(position));
    
    public void UpdatePointer(Point? pointerPosition)
    {
        if (pointerPosition is { } newPosition &&
            _selectedObject is not null && 
            _pointerPressed)
        {
            _commands.Enqueue(new MoveObjectCommand(_selectedObject, newPosition));
        }
        
        _pointerPosition = pointerPosition;
    }

    public void Update(in ChartFrame chartFrame)
    {
        Flush(chartFrame);

        _hoveredObject = null;
        (Point, DateTime)? hoveredPositionTime = null;

        if (_pointerPosition.HasValue)
        {
            var segment = chartFrame.FindSegment(_pointerPosition.Value);

            if (segment.HasValue)
            {
                hoveredPositionTime = (_pointerPosition.Value, segment.Value.Bar.Time);
            }
        }
        
        foreach (var chartObject in _chartObjects)
        {
            if (hoveredPositionTime.HasValue &&
                chartObject.Hover(chartFrame, hoveredPositionTime.Value.Item1, hoveredPositionTime.Value.Item2))
            {
                _hoveredObject = chartObject;
            }

            chartObject.Draw(chartFrame);
        }
    }

    private void Flush(in ChartFrame chartFrame)
    {
        while (_commands.TryDequeue(out var command))
        {
            switch (command)
            {
                case AddHorizontalLineCommand addHorizontalLineCommand:
                    _chartObjects.Add(new HorizontalLine()
                    {
                        Price = (decimal)chartFrame.PosYToPrice(addHorizontalLineCommand.Position.Y)
                    });
                    break;
                
                case AddHorizontalRayCommand addHorizontalRayCommand:
                    var segment = chartFrame.FindSegmentOrFail(addHorizontalRayCommand.Position);

                    _chartObjects.Add(new HorizontalRay()
                    {
                        Price = (decimal)chartFrame.PosYToPrice(addHorizontalRayCommand.Position.Y),
                        Time = segment.Bar.Time
                    });
                    
                    break;
                
                case DeleteObjectCommand deleteObjectCommand:
                    _chartObjects.Remove(deleteObjectCommand.ChartObject);
                    break;
                
                case MoveObjectCommand moveObjectCommand:
                    switch(moveObjectCommand.ChartObject)
                    {
                        case HorizontalLine horizontalLine:
                        {
                            var currentPositionY = chartFrame.PriceToPosY(horizontalLine.Price);
                            var newPrice = chartFrame.PosYToPrice(currentPositionY + moveObjectCommand.NewPosition.Y);
                            horizontalLine.Price = (decimal)newPrice;
                            break;
                        }
                        
                        case HorizontalRay horizontalRay:
                        {
                            var newPrice = chartFrame.PosYToPrice(moveObjectCommand.NewPosition.Y);
                            var newSegment = chartFrame.FindSegmentOrFail(moveObjectCommand.NewPosition);

                            horizontalRay.Price = (decimal)newPrice;
                            horizontalRay.Time = newSegment.Bar.Time;
                            break;
                        }
                    }
                    break;
                
                default: throw new NotSupportedException();
            }
        }
    }

    public void KeyDown(KeyEventArgs args)
    {
        if (args.Key is not Key.Back)
        {
            return;
        }

        if (_selectedObject is not null)
        {
            _commands.Enqueue(new DeleteObjectCommand(_selectedObject));
            _selectedObject?.Unselect();
            _selectedObject = null;
        }
    }

    public void PointerPressed(Point pointerPosition)
    {
        if (_hoveredObject is not null)
        {
            _selectedObject = _hoveredObject;
            _selectedObject.Select();
        }
        else
        {
            _selectedObject?.Unselect();
            _selectedObject = null;
        }

        _pointerPressed = true;
    }

    public void PointerReleased(Point pointerPosition)
    {
        _pointerPressed = false;
    }
}