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
    
    private readonly ConcurrentQueue<Command> _commands = new();
    private readonly List<IChartObject> _chartObjects = new();
    private Point? _pointerPosition;
    private IChartObject? _hoveredObject;
    private IChartObject? _selectedObject;

    public void CreateHorizontalLine(Point position)
    {
        _commands.Enqueue(new AddHorizontalLineCommand(position));
    }

    public void UpdatePointer(Point? pointerPosition)
    {
        _pointerPosition = pointerPosition;
    }

    public void Update(in ChartFrame chartFrame)
    {
        Flush(chartFrame);

        _hoveredObject = null;

        foreach (var chartObject in _chartObjects)
        {
            if (_pointerPosition.HasValue && chartObject.Hover(chartFrame, _pointerPosition.Value))
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
                
                case DeleteObjectCommand deleteObjectCommand:
                    _chartObjects.Remove(deleteObjectCommand.ChartObject);
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
    }
}