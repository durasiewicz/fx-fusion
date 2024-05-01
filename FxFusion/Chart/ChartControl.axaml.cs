using System;
using System.Globalization;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using FxFusion.Data;
using FxFusion.Indicators;
using FxFusion.Models;
using ReactiveUI;
using SkiaSharp;

namespace FxFusion.Chart;

public partial class ChartControl : UserControl
{
    private readonly ChartDrawOperation _chartDrawOperation;
    private Bar[]? _data;
    private IMarketDataSource? _marketMarketDataSource;
    private int _availableBarsCount;
    private int _barsShift;
    private IIndicator _priceIndicator;
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

    public IIndicator PriceIndicator
    {
        get => _priceIndicator;
        set => SetAndRaise(PriceIndicatorProperty, ref _priceIndicator, value);
    }

    public static readonly DirectProperty<ChartControl, IIndicator> PriceIndicatorProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, IIndicator>(
            nameof(AvailableBarsCount),
            o => o.PriceIndicator,
            (o, v) => o.PriceIndicator = v,
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
            bool isPointerOverControl,
            IIndicator priceIndicator)
        {
            Data = data;
            Bounds = bounds;
            DataShift = dataShift;
            PointerPosition = pointerPosition;
            IsPointerOverControl = isPointerOverControl;
            _priceIndicator = priceIndicator ?? new CandlePriceIndicator();
        }

        private bool IsPointerOverControl { get; set; }
        private Point PointerPosition { get; set; }
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

            var chartFrame = new ChartFrame(canvas,
                Bounds,
                _settings,
                minPrice,
                maxPrice);
            
            var timeLabelFormattedText = new FormattedText(DateTime.Now.ToString("yyyy-MM-dd"),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                AppSettings.ScaleLabelTextPaint.TextSize,
                null);

            var lastTimeLabelPosX = Bounds.Width;
            var timeLabelPosY = (float)(Bounds.Height - (_settings.MarginBottom) + 12);
            var scaleYPosY = (float)(Bounds.Height - (_settings.MarginBottom + 5));
            
            for (var segmentIndex = 0; segmentIndex < visibleSegmentsCount; segmentIndex++)
            {
                if (segmentIndex >= visibleDataSpan.Length)
                {
                    continue;
                }

                var chartSegment = new ChartSegment(visibleDataSpan[segmentIndex],
                    currentSegmentPosX,
                    _segmentWidth);

                _priceIndicator.Draw(chartFrame, chartSegment);

                if (IsPointerOverControl &&
                    PointerPosition.X <= currentSegmentPosX &&
                    PointerPosition.X >= currentSegmentPosX - _segmentWidth)
                {
                    hoveredPosTime = (currentSegmentPosX - _segmentWidth / 2, chartSegment.Bar.Time);
                }

                if ((chartSegment.PosX - chartSegment.Width) + timeLabelFormattedText.Width < lastTimeLabelPosX - 10)
                {
                    // canvas.DrawLine(new SKPoint(currentSegmentPosX, scaleYPosY),
                    //     new SKPoint(currentSegmentPosX, scaleYPosY + 10),
                    //     AppSettings.ScaleBorderPaint);
                    
                    canvas.DrawText(chartSegment.Bar.Time.ToString("yyyy-MM-dd"),
                        (chartSegment.PosX - chartSegment.Width) ,
                        timeLabelPosY,
                        AppSettings.ScaleTextPaint);

                    lastTimeLabelPosX = (chartSegment.PosX - chartSegment.Width);
                }
                
                currentSegmentPosX -= _segmentWidth;
            }

            if (IsCrosshairVisible && IsPointerOverControl)
            {
                canvas.DrawLine(new SKPoint(0, (float)PointerPosition.Y - 0.5f),
                    new SKPoint((float)Bounds.Width, (float)PointerPosition.Y - 0.5f),
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

                if (IsCrosshairVisible && IsPointerOverControl)
                {
                    canvas.DrawRect(new SKRect(scaleBorderX,
                        (float)(PointerPosition.Y - 10.5f),
                        (float)Bounds.Width,
                        (float)(PointerPosition.Y + 10.5f)),  AppSettings.ScaleBorderPaint);

                    canvas.DrawText(chartFrame.PosYToPrice(PointerPosition.Y).ToString("0.##"),
                        scaleBorderX + 10,
                        (float)PointerPosition.Y + 5,
                        AppSettings.ScaleLabelTextPaint);
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
            IsPointerOver,
            _priceIndicator);

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