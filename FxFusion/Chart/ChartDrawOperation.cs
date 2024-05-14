using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using FxFusion.Indicators;
using FxFusion.Models;
using SkiaSharp;

namespace FxFusion.Chart;

public partial class ChartControl
{
    private class ChartDrawOperation : ICustomDrawOperation
    {
        public ChartDrawOperation(ChartObjectManager chartObjectManager, ChartScale chartScale)
        {
            _chartObjectManager = chartObjectManager;
            _chartScale = chartScale;
        }

        public void Dispose()
        {
        }

        public void BeginFrame(Bar[]? data,
            int dataShift,
            Rect bounds,
            IIndicator priceIndicator)
        {
            Data = data;
            Bounds = bounds;
            DataShift = dataShift;
            _priceIndicator = priceIndicator ?? new CandlePriceIndicator();
        }

        public void UpdatePointer(Point? pointerPosition)
        {
            _pointerPosition = pointerPosition;
        }

        private Point? _pointerPosition;
        private Bar[]? Data { get; set; }
        private int DataShift { get; set; }

        public Rect Bounds { get; private set; }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;
        
        private int _segmentWidth = 50;
        private readonly int _zoomStep = 2;

        public void ZoomIn() => _segmentWidth = Math.Min(80, _segmentWidth + _zoomStep);
        public void ZoomOut() => _segmentWidth = Math.Max(2, _segmentWidth - _zoomStep);

        private IIndicator _priceIndicator = new CandlePriceIndicator();
        private readonly List<ChartSegment> _visibleChartSegments = new();
        private readonly ChartObjectManager _chartObjectManager;
        private readonly ChartScale _chartScale;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();

            if (leaseFeature is null)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            canvas.Clear(SKColors.Moccasin);

            if (Data is null)
            {
                var text = "No data to display.";
                var defaultTypeface = new Typeface(FontFamily.Default);

                var formattedText = new FormattedText(text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    defaultTypeface,
                    AppSettings.ChartInfoPaint.TextSize,
                    null);

                canvas.DrawText(text,
                    (float)(Bounds.Width / 2 - formattedText.Width / 2),
                    ((float)Bounds.Height / 2),
                    AppSettings.ChartInfoPaint);

                return;
            }

            var canvasBounds = new Rect(Bounds.X + 100, Bounds.Y + 100, Bounds.Width - 300, Bounds.Height - 300);
            var chartBounds = _chartScale.AdjustChartBounds(canvasBounds);
            
            var visibleSegmentsCount = (int)(chartBounds.Width / _segmentWidth);
            var visibleDataSpan = Data.AsSpan()[DataShift..Math.Min(DataShift + visibleSegmentsCount, Data.Length)];
            var (minPrice, maxPrice) = CalculateMinMaxPrice(visibleDataSpan);

            // 0.5f is initial value for pixel perfect drawing
            var currentSegmentPosX = (float)(chartBounds.X + chartBounds.Width) - 0.5f;

            (float, DateTime)? hoveredPosTime = null;
            
            _visibleChartSegments.Clear();
            
            for (var segmentIndex = 0; segmentIndex < visibleSegmentsCount; segmentIndex++)
            {
                if (segmentIndex >= visibleDataSpan.Length)
                {
                    continue;
                }

                var chartSegment = new ChartSegment(segmentIndex,
                    visibleDataSpan[segmentIndex],
                    currentSegmentPosX - _segmentWidth,
                    currentSegmentPosX);
                
                _visibleChartSegments.Add(chartSegment);
              
                currentSegmentPosX -= _segmentWidth;
            }
            
            var chartFrame = new ChartFrame(canvas,
                canvasBounds,
                chartBounds,
                minPrice,
                maxPrice,
                visibleDataSpan.IsEmpty ? default : visibleDataSpan[^1].Time,
                visibleDataSpan.IsEmpty ? default : visibleDataSpan[0].Time,
                _visibleChartSegments);

            for (var segmentIndex = 0; segmentIndex < _visibleChartSegments.Count; segmentIndex++)
            {
                _priceIndicator.Draw(chartFrame, segmentIndex);
            }

            _chartScale.Draw(in chartFrame, _pointerPosition);
            _chartObjectManager.Update(in chartFrame);
            
            canvas.Restore();

            return;

            (decimal min, decimal max) CalculateMinMaxPrice(ReadOnlySpan<Bar> dataSlice)
            {
                var currentMin = decimal.MaxValue;
                var currentMax = decimal.MinValue;

                foreach (var bar in dataSlice)
                {
                    currentMin = Math.Min(currentMin, bar.Low);
                    currentMax = Math.Max(currentMax, bar.High);
                }

                if (currentMin is decimal.MaxValue)
                {
                    currentMin = 0;
                }

                if (currentMax is decimal.MinValue)
                {
                    currentMax = 0;
                }

                return (currentMin, currentMax);
            }
        }
    }
}