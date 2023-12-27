using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using FxFusion.Models;
using SkiaSharp;

namespace FxFusion.Controls;

public partial class ChartControl : UserControl
{
    private readonly ChartDrawOperation _chartDrawOperation;

    public ChartControl()
    {
        InitializeComponent();
        ClipToBounds = true;
        _chartDrawOperation = new ChartDrawOperation(GetTestData().Reverse().ToArray());
    }

    private IEnumerable<Bar> GetTestData() =>
        from cells in (from line in File.ReadAllLines("usdjpy_d.csv").Skip(1) select line.Split(','))
        let decimalCulture = new CultureInfo("en-US")
        select new Bar(decimal.Parse(cells[1], decimalCulture),
            decimal.Parse(cells[2], decimalCulture),
            decimal.Parse(cells[3], decimalCulture),
            decimal.Parse(cells[4], decimalCulture),
            DateTime.Parse(cells[0]));
    
    private class ChartDrawOperation(Bar[] data) : ICustomDrawOperation
    {
        public void Dispose() { }
        public void BeginRender(Rect bounds) => Bounds = bounds;
        public Rect Bounds { get; private set; }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        private readonly SKPaint _barPaint = new()
        {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke
        };
        
        private readonly SKPaint _bullCandlePaint = new()
        {
            IsAntialias = true,
            Color = SKColors.Green,
            Style = SKPaintStyle.Fill
        };
        
        private readonly SKPaint _bearCandlePaint = new()
        {
            IsAntialias = true,
            Color = SKColors.Crimson,
            Style = SKPaintStyle.Fill
        };

        private readonly int _segmentWidth = 50;
        private readonly int _segmentMargin = 1;
        
        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            
            if (leaseFeature is null)
            {
                return;
            }

            var visibleSegmentsCount = (int)(Bounds.Width / _segmentWidth);
            var visibleDataSpan = data.AsSpan()[..(visibleSegmentsCount + 1)];
            var (minPrice, maxPrice) = CalculateMinMaxPrice(visibleDataSpan);
            var priceRange = maxPrice - minPrice;
            var pixelPerPriceUnit = Bounds.Height / (double)priceRange;
            
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            canvas.Clear(SKColors.Moccasin);

            // 0.5f is initial value for pixel perfect drawing
            var currentSegmentPosX = 0.5f;

            for (var segmentIndex = 0; segmentIndex < visibleSegmentsCount; segmentIndex++)
            {
                var barData = visibleDataSpan[visibleSegmentsCount - segmentIndex];
                var segmentMiddle = currentSegmentPosX - (_segmentWidth / 2);
                
                canvas.DrawLine(new SKPoint(segmentMiddle, CalculateY(barData.High)),
                    new SKPoint(segmentMiddle, CalculateY(barData.Low)),
                    _barPaint);

                canvas.DrawRect(new SKRect(currentSegmentPosX + _segmentMargin,
                    CalculateY(Math.Max(barData.Open, barData.Close)),
                    currentSegmentPosX + _segmentWidth - _segmentMargin,
                    CalculateY(Math.Min(barData.Open, barData.Close))),
                    barData.Open > barData.Close ? _bullCandlePaint : _bearCandlePaint);
                
                currentSegmentPosX += _segmentWidth;
            }
            
            canvas.Restore();
            
            return;

            float CalculateY(decimal price)
            {
                var priceDataMinDiff = (int)(price - minPrice);
                return (float)(Bounds.Height - priceDataMinDiff * pixelPerPriceUnit);
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

                return (currentMin, currentMax);
            }
        }
    }
    
    public override void Render(DrawingContext context)
    {
        _chartDrawOperation.BeginRender(new Rect(0, 0, Bounds.Width, Bounds.Height));
        context.Custom(_chartDrawOperation);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }
}