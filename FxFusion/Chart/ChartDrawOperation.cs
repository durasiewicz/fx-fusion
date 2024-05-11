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
        public event Action<ChartMode> ChartModeChanged; 
        
        public ChartDrawOperation(ChartObjectManager chartObjectManager)
        {
            _chartObjectManager = chartObjectManager;
        }

        public void Dispose()
        {
        }

        public void BeginRender(Bar[]? data,
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

        public bool IsCrosshairVisible { get; set; }

        private int _segmentWidth = 50;
        private readonly int _zoomStep = 2;

        public void ZoomIn() => _segmentWidth = Math.Min(80, _segmentWidth + _zoomStep);
        public void ZoomOut() => _segmentWidth = Math.Max(2, _segmentWidth - _zoomStep);
        
        private readonly ChartSettings _settings = new();

        private IIndicator _priceIndicator = new CandlePriceIndicator();
        private readonly List<ChartSegment> _visibleChartSegments = new();
        private readonly ChartObjectManager _chartObjectManager;

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

            var visibleSegmentsCount = (int)((Bounds.Width - _settings.MarginRight) / _segmentWidth);
            var visibleDataSpan = Data.AsSpan()[DataShift..Math.Min(DataShift + visibleSegmentsCount, Data.Length)];
            var (minPrice, maxPrice) = CalculateMinMaxPrice(visibleDataSpan);

            // 0.5f is initial value for pixel perfect drawing
            var currentSegmentPosX = (float)Bounds.Width - _settings.MarginRight - 0.5f;

            (float, DateTime)? hoveredPosTime = null;
            
            var timeLabelFormattedText = new FormattedText(DateTime.Now.ToString("yyyy-MM-dd"),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                AppSettings.ScaleLabelTextPaint.TextSize,
                null);

            var lastTimeLabelPosX = Bounds.Width;
            var timeLabelPosY = (float)(Bounds.Height - (_settings.MarginBottom) + 12);

            _visibleChartSegments.Clear();

            ChartSegment? hoveredSegment = null;
            
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

                if (_pointerPosition?.X <= currentSegmentPosX &&
                    _pointerPosition?.X >= currentSegmentPosX - _segmentWidth)
                {
                    hoveredPosTime = (currentSegmentPosX - _segmentWidth / 2, chartSegment.Bar.Time);
                }

                currentSegmentPosX -= _segmentWidth;
            }
            
            var chartFrame = new ChartFrame(canvas,
                Bounds,
                _settings,
                minPrice,
                maxPrice,
                visibleDataSpan.IsEmpty ? default : visibleDataSpan[^1].Time,
                visibleDataSpan.IsEmpty ? default : visibleDataSpan[0].Time,
                _visibleChartSegments);

            for (int segmentIndex = 0; segmentIndex < _visibleChartSegments.Count; segmentIndex++)
            {
                var chartSegment = _visibleChartSegments[segmentIndex];
                _priceIndicator.Draw(chartFrame, segmentIndex);
                
                if (chartSegment.LeftBorderPosX + timeLabelFormattedText.Width < lastTimeLabelPosX - 10)
                {
                    // canvas.DrawLine(new SKPoint(currentSegmentPosX, scaleYPosY),
                    //     new SKPoint(currentSegmentPosX, scaleYPosY + 10),
                    //     AppSettings.ScaleBorderPaint);
                
                    canvas.DrawText(chartSegment.Bar.Time.ToString("yyyy-MM-dd"),
                        chartSegment.LeftBorderPosX,
                        timeLabelPosY,
                        AppSettings.ScaleTextPaint);
                
                    lastTimeLabelPosX = chartSegment.LeftBorderPosX;
                }
            }

            if (IsCrosshairVisible && _pointerPosition.HasValue)
            {
                canvas.DrawLine(new SKPoint(0, (float)_pointerPosition.Value.Y - 0.5f),
                    new SKPoint((float)Bounds.Width, (float)_pointerPosition.Value.Y - 0.5f),
                    AppSettings.ScaleBorderPaint);

                if (hoveredPosTime is not null)
                {
                    var (posX, _) = hoveredPosTime.Value;
                    canvas.DrawLine(new SKPoint(posX, 0),
                        new SKPoint(posX, (float)Bounds.Height),
                        AppSettings.ScaleBorderPaint);
                }
            }

            DrawYScale();
            DrawXScale();

            _chartObjectManager.Update(chartFrame);

            canvas.Restore();

            return;

            double CalculateYScaleStep(double range)
            {
                var visibleSteps = Bounds.Height / 20;
                var roughStep = range / (visibleSteps - 1);
                var exponent = Math.Floor(Math.Log10(roughStep));
                var magnitude = Math.Pow(10, exponent);
                var fraction = roughStep / magnitude;

                fraction = fraction switch
                {
                    < 2 => 1,
                    < 3 => 2,
                    < 6 => 3,
                    < 7 => 5,
                    < 8 => 6,
                    _ => 10
                };

                return fraction * Math.Pow(10, exponent);
            }

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

            void DrawXScale()
            {
                var posY = (float)(Bounds.Height - (_settings.MarginBottom + 5));

                canvas.DrawLine(new SKPoint(0, posY),
                    new SKPoint((float)Bounds.Width, posY),
                    AppSettings.ScaleBorderPaint);

                if (hoveredPosTime is not null && IsCrosshairVisible)
                {
                    var (posX, time) = hoveredPosTime.Value;

                    var timeLabelText = time.ToString("yyyy-MM-dd");
                    var defaultTypeface = new Typeface(FontFamily.Default);

                    var formattedText = new FormattedText(timeLabelText,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        defaultTypeface,
                        AppSettings.ScaleLabelTextPaint.TextSize,
                        null);

                    var textHalfWidth = (float)(formattedText.Width / 2);
                    var leftRightPadding = 10f;

                    canvas.DrawRect(new SKRect(posX - textHalfWidth - leftRightPadding,
                            posY,
                            posX + textHalfWidth + leftRightPadding,
                            (float)Bounds.Height),
                        AppSettings.ScaleBorderPaint);

                    canvas.DrawText(timeLabelText,
                        posX - textHalfWidth,
                        posY + 18,
                        AppSettings.ScaleLabelTextPaint);
                }
            }

            void DrawYScale()
            {
                var scaleYStep = CalculateYScaleStep((double)maxPrice);
                var scaleYMax = Math.Floor((float)maxPrice / scaleYStep) * scaleYStep;
                var scaleYMin = Math.Floor((float)minPrice / scaleYStep) * scaleYStep;
                var currentPrice = scaleYMax;

                var scaleBorderX = (float)Bounds.Width - _settings.MarginRight + 5 + 0.5f;
                var scaleBottomY = (float)(Bounds.Height - _settings.MarginBottom - 5);

                canvas.DrawLine(new SKPoint(scaleBorderX, 0),
                    new SKPoint(scaleBorderX, scaleBottomY),
                    AppSettings.ScaleBorderPaint);

                while (currentPrice >= scaleYMin)
                {
                    var posY = chartFrame.PriceToPosY((decimal)currentPrice);

                    if (posY >= scaleBottomY)
                    {
                        break;
                    }

                    canvas.DrawLine(new SKPoint(scaleBorderX, posY),
                        new SKPoint(scaleBorderX + 5, posY),
                        AppSettings.ScaleBorderPaint);

                    canvas.DrawText(currentPrice.ToString("0.##"),
                        ((float)Bounds.Width - _settings.MarginRight + 15),
                        posY,
                        AppSettings.ScaleBorderPaint);

                    currentPrice -= scaleYStep;
                }

                if (IsCrosshairVisible && _pointerPosition.HasValue)
                {
                    canvas.DrawRect(new SKRect(scaleBorderX,
                        (float)(_pointerPosition.Value.Y - 10.5f),
                        (float)Bounds.Width,
                        (float)(_pointerPosition.Value.Y + 10.5f)), AppSettings.ScaleBorderPaint);

                    canvas.DrawText(chartFrame.PosYToPrice(_pointerPosition.Value.Y).ToString("0.##"),
                        scaleBorderX + 10,
                        (float)_pointerPosition.Value.Y + 5,
                        AppSettings.ScaleLabelTextPaint);
                }
            }
        }
    }
}