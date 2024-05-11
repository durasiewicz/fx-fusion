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

    private record MoveObjectCommand(IChartObject ChartObject, Point StartPosition, Point NewPosition) : Command;

    private readonly ConcurrentQueue<Command> _commands = new();
    private readonly List<IChartObject> _chartObjects = new();
    private Point? _pointerPosition;
    private IChartObject? _hoveredObject;
    private IChartObject? _selectedObject;
    private Point? _dragStartPosition;

    public void CreateHorizontalLine(Point position) => _commands.Enqueue(new AddHorizontalLineCommand(position));
    public void CreateHorizontalRay(Point position) => _commands.Enqueue(new AddHorizontalRayCommand(position));

    public void UpdatePointer(Point? pointerPosition)
    {
        if (pointerPosition is { } newPosition &&
            _dragStartPosition is { } startPosition &&
            _selectedObject is not null)
        {
            _commands.Enqueue(new MoveObjectCommand(_selectedObject, startPosition, newPosition));
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
            if (hoveredPositionTime is var (pos, time) && chartObject.Hover(chartFrame, pos, time))
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
                    if (moveObjectCommand.NewPosition.X < 0 || moveObjectCommand.NewPosition.Y < 0)
                    {
                        break;
                    }

                    switch (moveObjectCommand.ChartObject)
                    {
                        case HorizontalLine horizontalLine:
                        {
                            var newPrice = chartFrame.PosYToPrice(moveObjectCommand.NewPosition.Y);
                            horizontalLine.Price = (decimal)newPrice;
                            break;
                        }

                        case HorizontalRay horizontalRay:
                        {
                            var newPrice = chartFrame.PosYToPrice(moveObjectCommand.NewPosition.Y);

                            var startSegment = chartFrame.FindSegmentOrFail(moveObjectCommand.StartPosition);
                            var newSegment = chartFrame.FindSegment(moveObjectCommand.NewPosition);

                            if (newSegment is null)
                            {
                                return;
                            }
                            
                            var segmentIndexDelta = newSegment.Value.SegmentIndex - startSegment.SegmentIndex;

                            horizontalRay.Price = (decimal)newPrice;

                            if (segmentIndexDelta != 0)
                            {
                                horizontalRay.DragStartTime ??= horizontalRay.Time;

                                var currentSegment = chartFrame.FindSegmentOrFail(horizontalRay.DragStartTime.Value);
                                var newSegmentIndex = currentSegment.SegmentIndex + segmentIndexDelta;

                                if (newSegmentIndex < 0)
                                {
                                    newSegmentIndex = 0;
                                }

                                if (newSegmentIndex >= chartFrame.Segments.Count)
                                {
                                    newSegmentIndex = chartFrame.Segments.Count - 1;
                                }

                                horizontalRay.Time = chartFrame.Segments[newSegmentIndex].Bar.Time;
                            }

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

        _dragStartPosition = pointerPosition;
    }

    public void PointerReleased(Point pointerPosition)
    {
        _dragStartPosition = null;

        foreach (var chartObject in _chartObjects)
        {
            if (chartObject is HorizontalRay ray)
            {
                ray.DragStartTime = null;
            }
        }
    }
}