using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Dialogs;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using FxFusion.Data;
using FxFusion.Models;
using ReactiveUI;
using SkiaSharp;

namespace FxFusion.Controls;

public partial class ChartControl : UserControl
{
    private readonly ChartDrawOperation _chartDrawOperation;
    private Bar[]? _data;
    private IMarketDataSource? _marketMarketDataSource;
    private int _availableBarsCount;
    private int _barsShift;
    private string? _timeFrame;
    private string? _symbol;

    public ChartControl()
    {
        InitializeComponent();
        ClipToBounds = true;
        _chartDrawOperation = new ChartDrawOperation();

        PropertyChanged += async (sender, args) =>
        {
            if (args.Property.Name is nameof(Symbol) or nameof(TimeFrame))
            {
                await LoadData();
            }
        };

        PointerMoved += (sender, args) => _pointerPosition = args.GetPosition(this);
        PointerWheelChanged += (sender, args) =>
        {
            switch (args.Delta)
            {
                case { Y: > 0 }:
                    _chartDrawOperation.ZoomIn();
                    break;

                case { Y: < 0 }:
                    _chartDrawOperation.ZoomOut();
                    break;
            }
        };

        ZoomInCommand = ReactiveCommand.Create(_chartDrawOperation.ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(_chartDrawOperation.ZoomOut);
    }

    private async Task LoadData()
    {
        if (_marketMarketDataSource is null)
        {
            return;
        }

        _data = await _marketMarketDataSource.GetData(Symbol, TimeFrame);

        AvailableBarsCount = _data.Length;
        BarsShift = _data.Length;
    }

    public IMarketDataSource? MarketDataSource
    {
        get => _marketMarketDataSource;
        set => SetAndRaise(MarketDataSourceProperty, ref _marketMarketDataSource, value);
    }

    public static readonly DirectProperty<ChartControl, IMarketDataSource?> MarketDataSourceProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, IMarketDataSource?>(
            nameof(MarketDataSource),
            o => o.MarketDataSource,
            (o, v) => o.MarketDataSource = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public string? Symbol
    {
        get => _symbol;
        set => SetAndRaise(SymbolProperty, ref _symbol, value);
    }

    public static readonly DirectProperty<ChartControl, string?> SymbolProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, string?>(
            nameof(Symbol),
            o => o.Symbol,
            (o, v) => o.Symbol = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public string? TimeFrame
    {
        get => _timeFrame;
        set => SetAndRaise(TimeFrameProperty, ref _timeFrame, value);
    }

    public static readonly DirectProperty<ChartControl, string?> TimeFrameProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, string?>(
            nameof(TimeFrame),
            o => o.TimeFrame,
            (o, v) => o.TimeFrame = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public int BarsShift
    {
        get => _barsShift;
        set => SetAndRaise(BarsShiftProperty, ref _barsShift, value);
    }

    public static readonly DirectProperty<ChartControl, int> BarsShiftProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, int>(
            nameof(BarsShift),
            o => o._barsShift,
            (o, v) => o._barsShift = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public int AvailableBarsCount
    {
        get => _availableBarsCount;
        set => SetAndRaise(AvailableBarsCountProperty, ref _availableBarsCount, value);
    }

    public static readonly DirectProperty<ChartControl, int> AvailableBarsCountProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, int>(
            nameof(AvailableBarsCount),
            o => o.AvailableBarsCount,
            (o, v) => o.AvailableBarsCount = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    private Point _pointerPosition;

    private class ChartDrawOperation : ICustomDrawOperation
    {
        public void Dispose()
        {
        }

        public void BeginRender(Bar[]? data,
            int dataShift,
            Rect bounds,
            Point pointerPosition,
            bool isPointerOverControl)
        {
            Data = data;
            Bounds = bounds;
            DataShift = dataShift;
            PointerPosition = pointerPosition;
            IsPointerOverControl = isPointerOverControl;
        }

        private bool IsPointerOverControl { get; set; }
        private Point PointerPosition { get; set; }
        private Bar[]? Data { get; set; }
        private int DataShift { get; set; }

        public Rect Bounds { get; private set; }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        public bool IsCrosshairVisible { get; set; }

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

        private readonly SKPaint _scalePaint = new()
        {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            FilterQuality = SKFilterQuality.High,
        };

        private readonly SKPaint _scaleLabelText = new()
        {
            IsAntialias = true,
            Color = SKColors.White,
            //Style = SKPaintStyle.Stroke,
            FilterQuality = SKFilterQuality.High,
        };

        private int _segmentWidth = 50;
        private readonly int _zoomStep = 2;

        public void ZoomIn() => _segmentWidth = Math.Min(80, _segmentWidth + _zoomStep);
        public void ZoomOut() => _segmentWidth = Math.Max(2, _segmentWidth - _zoomStep);

        private int SegmentMargin => (int)(_segmentWidth * 0.1);
        private readonly int _marginTop = 20;
        private readonly int _marginBottom = 20;
        private readonly int _marginRight = 50;

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

            var visibleSegmentsCount = (int)((Bounds.Width - _marginRight) / _segmentWidth);
            var visibleDataSpan = Data.AsSpan()[DataShift..Math.Min(DataShift + visibleSegmentsCount, Data.Length)];
            var (minPrice, maxPrice) = CalculateMinMaxPrice(visibleDataSpan);
            var priceRange = maxPrice - minPrice;
            var pixelPerPriceUnit = (Bounds.Height - _marginTop - _marginBottom) / (double)priceRange;

            // 0.5f is initial value for pixel perfect drawing
            var currentSegmentPosX = (float)Bounds.Width - _marginRight - 0.5f;

            (float, DateTime)? hoveredPosTime = null;

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

                if (IsPointerOverControl &&
                    PointerPosition.X <= currentSegmentPosX &&
                    PointerPosition.X >= currentSegmentPosX - _segmentWidth)
                {
                    hoveredPosTime = (segmentMiddle, barData.Time);
                }

                currentSegmentPosX -= _segmentWidth;
            }

            if (IsCrosshairVisible && IsPointerOverControl)
            {
                canvas.DrawLine(new SKPoint(0, (float)PointerPosition.Y - 0.5f),
                    new SKPoint((float)Bounds.Width, (float)PointerPosition.Y - 0.5f),
                    _barPaint);

                if (hoveredPosTime is not null)
                {
                    var (posX, _) = hoveredPosTime.Value;
                    canvas.DrawLine(new SKPoint(posX, 0),
                        new SKPoint(posX, (float)Bounds.Height),
                        _barPaint);
                }
            }

            DrawYScale();
            DrawXScale();

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

            float CalculateY(decimal price)
            {
                var priceDataMinDiff = price - minPrice;
                return (float)(Bounds.Height - _marginTop - _marginBottom -
                               (double)(priceDataMinDiff * (decimal)pixelPerPriceUnit)) + (_marginTop / 2);
            }

            float CalculatePriceFromY(double posY)
            {
                var priceDataMinDiff = (Bounds.Height - _marginTop - posY) / (float)pixelPerPriceUnit;
                return (float)(minPrice + (decimal)priceDataMinDiff);
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
                var posY = (float)(Bounds.Height - (_marginBottom + 5));

                canvas.DrawLine(new SKPoint(0, posY),
                    new SKPoint((float)Bounds.Width, posY),
                    _barPaint);

                if (hoveredPosTime is not null && IsCrosshairVisible)
                {
                    var (posX, time) = hoveredPosTime.Value;

                    canvas.DrawRect(new SKRect(posX - 60,
                            posY,
                            posX + 60,
                            (float)Bounds.Height),
                        _scalePaint);

                    canvas.DrawText(time.ToString(),
                        posX - 50,
                        posY + 18,
                        _scaleLabelText);
                }
            }

            void DrawYScale()
            {
                var scaleYStep = CalculateYScaleStep((double)maxPrice);
                var scaleYMax = Math.Floor((float)maxPrice / scaleYStep) * scaleYStep;
                var scaleYMin = Math.Floor((float)minPrice / scaleYStep) * scaleYStep;
                var currentPrice = scaleYMax;

                var scaleBorderX = (float)Bounds.Width - _marginRight + 5 + 0.5f;

                canvas.DrawLine(new SKPoint(scaleBorderX, 0),
                    new SKPoint(scaleBorderX, (float)Bounds.Height),
                    _barPaint);

                while (currentPrice >= scaleYMin)
                {
                    var posY = CalculateY((decimal)currentPrice);

                    canvas.DrawLine(new SKPoint(scaleBorderX, posY),
                        new SKPoint(scaleBorderX + 5, posY),
                        _barPaint);

                    canvas.DrawText(currentPrice.ToString("0.##"),
                        ((float)Bounds.Width - _marginRight + 15),
                        posY,
                        _scalePaint);

                    currentPrice -= scaleYStep;
                }

                if (IsCrosshairVisible && IsPointerOverControl)
                {
                    canvas.DrawRect(new SKRect(scaleBorderX,
                        (float)(PointerPosition.Y - 10.5f),
                        (float)Bounds.Width,
                        (float)(PointerPosition.Y + 10.5f)), _scalePaint);

                    canvas.DrawText(CalculatePriceFromY(PointerPosition.Y).ToString("0.##"),
                        scaleBorderX + 10,
                        (float)PointerPosition.Y + 5,
                        _scaleLabelText);
                }
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        var dataShift = _data?.Length - BarsShift ?? 0;

        _chartDrawOperation.BeginRender(_data,
            dataShift,
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            _pointerPosition,
            IsPointerOver);

        context.Custom(_chartDrawOperation);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    public bool IsCrosshairVisible
    {
        get => _chartDrawOperation.IsCrosshairVisible;
        set => _chartDrawOperation.IsCrosshairVisible = value;
    }

    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
}