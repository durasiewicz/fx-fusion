using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using FxFusion.Data;
using FxFusion.Models;
using SkiaSharp;

namespace FxFusion.Controls;

public partial class ChartControl : UserControl
{
    private readonly ChartDrawOperation _chartDrawOperation;
    private readonly IMarketDataSource<string, string> _dataSource;
    private Bar[]? _data;

    public ChartControl()
    {
        InitializeComponent();
        ClipToBounds = true;
        _chartDrawOperation = new ChartDrawOperation();
        _dataSource = new StooqMarketDataSource();
        SelectedSymbolComboBox.SelectionChanged += async (sender, args) =>
        {
            _data = null;
            await LoadData();
        };
        SelectedSymbolComboBox.ItemsSource = _dataSource.AvailableSymbols.OrderBy(q => q).ToArray();
        SelectedSymbolComboBox.SelectedIndex = 0;
    }

    private async Task LoadData()
    {
        _data = await _dataSource.GetData(SelectedSymbolComboBox.SelectedValue as string, "D");
        ChartScrollBar.Maximum = _data.Length;
        ChartScrollBar.Value = ChartScrollBar.Maximum;
    }

    private IEnumerable<Bar> GetTestData() =>
        from cells in (from line in File.ReadAllLines("usdjpy_d.csv").Skip(1) select line.Split(','))
        let decimalCulture = new CultureInfo("en-US")
        select new Bar(decimal.Parse(cells[1], decimalCulture),
            decimal.Parse(cells[2], decimalCulture),
            decimal.Parse(cells[3], decimalCulture),
            decimal.Parse(cells[4], decimalCulture),
            DateTime.Parse(cells[0]));

    private class ChartDrawOperation : ICustomDrawOperation
    {
        public void Dispose()
        {
        }

        public void BeginRender(Bar[]? data, int dataShift, Rect bounds)
        {
            Data = data;
            Bounds = bounds;
            DataShift = dataShift;
        }

        private Bar[]? Data { get; set; }
        private int DataShift { get; set; }

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

        private readonly SKPaint _candleBodyBorder = new()
        {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke
        };

        private int _segmentWidth = 50;
        private readonly int _zoomStep = 2;

        public void ZoomIn() => _segmentWidth = Math.Min(50, _segmentWidth + _zoomStep);
        public void ZoomOut() => _segmentWidth = Math.Max(10, _segmentWidth - _zoomStep);

        private int SegmentMargin => (int)(_segmentWidth * 0.1);
        private readonly int _marginTop = 50;
        private readonly int _marginBottom = 50;

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
                canvas.DrawText("No data to display.",
                    ((float)Bounds.Width / 2),
                    ((float)Bounds.Height / 2),
                    _barPaint);

                return;
            }

            var visibleSegmentsCount = (int)(Bounds.Width / _segmentWidth);
            var visibleDataSpan = Data.AsSpan()[DataShift..Math.Min(DataShift + visibleSegmentsCount, Data.Length)];
            var (minPrice, maxPrice) = CalculateMinMaxPrice(visibleDataSpan);
            var priceRange = maxPrice - minPrice;
            var pixelPerPriceUnit = (Bounds.Height - _marginTop - _marginBottom) / (double)priceRange;

            // 0.5f is initial value for pixel perfect drawing
            var currentSegmentPosX = _segmentWidth * visibleSegmentsCount - 0.5f;

            for (var segmentIndex = 0; segmentIndex < visibleSegmentsCount; segmentIndex++)
            {
                if (segmentIndex >= visibleDataSpan.Length)
                {
                    continue;
                }

                var barData = visibleDataSpan[segmentIndex];
                var segmentMiddle = currentSegmentPosX - (_segmentWidth / 2);

                var barHighY = CalculateY(barData.High);
                var barLowY = CalculateY(barData.Low);

                canvas.DrawLine(new SKPoint(segmentMiddle, barHighY),
                    new SKPoint(segmentMiddle, barLowY),
                    _barPaint);

                var bodyRect = new SKRect(currentSegmentPosX - _segmentWidth + SegmentMargin,
                    CalculateY(Math.Max(barData.Open, barData.Close)),
                    currentSegmentPosX - SegmentMargin,
                    CalculateY(Math.Min(barData.Open, barData.Close)));

                canvas.DrawRect(bodyRect, barData.Open > barData.Close ? _bearCandlePaint : _bullCandlePaint);
                canvas.DrawRect(bodyRect, _candleBodyBorder);

                // canvas.DrawText(barData.Time.ToString("dd-MM"),
                //     segmentMiddle,
                //     CalculateY(barData.High),
                //     _barPaint);

                currentSegmentPosX -= _segmentWidth;
            }

            canvas.Restore();

            return;

            float CalculateY(decimal price)
            {
                var priceDataMinDiff = price - minPrice;
                return (float)(Bounds.Height - _marginTop - (double)(priceDataMinDiff * (decimal)pixelPerPriceUnit));
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
        }
    }

    public override void Render(DrawingContext context)
    {
        var dataShift = _data?.Length - (int)ChartScrollBar.Value ?? 0;
        _chartDrawOperation.BeginRender(_data, dataShift, new Rect(0, 0, Bounds.Width, Bounds.Height));
        context.Custom(_chartDrawOperation);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    private void ZoomIn(object? sender, RoutedEventArgs e) => _chartDrawOperation.ZoomIn();
    private void ZoomOut(object? sender, RoutedEventArgs e) => _chartDrawOperation.ZoomOut();
}