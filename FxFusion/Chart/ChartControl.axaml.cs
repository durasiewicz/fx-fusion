using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FxFusion.Data;
using FxFusion.Models;
using ReactiveUI;

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
    private readonly ChartObjectManager _chartObjectManager;
    private readonly ChartScale _chartScale;

    public ChartControl()
    {
        InitializeComponent();
        ClipToBounds = true;
        _chartObjectManager = new ChartObjectManager();
        _chartScale = new ChartScale();
        _chartDrawOperation = new ChartDrawOperation(_chartObjectManager, _chartScale);

        PropertyChanged += async (sender, args) =>
        {
            if (args.Property.Name is nameof(Symbol) or nameof(TimeFrame))
            {
                await LoadData();
            }
        };

        PointerMoved += (sender, args) =>
        {
            var pointerPosition = IsPointerOver ? args.GetPosition(this) : (Point?)null;
            
            _chartObjectManager.UpdatePointer(pointerPosition);
            _chartDrawOperation.UpdatePointer(pointerPosition);
        };

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

        PointerReleased += (sender, args) =>
        {
            var pointerPosition = args.GetPosition(this);
            
            _chartObjectManager.PointerReleased(pointerPosition);
        };

        PointerPressed += (sender, args) =>
        {
            var pointerPosition = args.GetPosition(this);

            switch (ChartMode)
            {
                case ChartMode.AddHorizontalLine:
                    _chartObjectManager.CreateHorizontalLine(pointerPosition);
                
                    ChartMode = ChartMode.Default;
                    break;
                
                case ChartMode.AddHorizontalRay:
                    _chartObjectManager.CreateHorizontalRay(pointerPosition);
                    
                    ChartMode = ChartMode.Default;
                    break;
                
                default:
                    _chartObjectManager.PointerPressed(pointerPosition);
                    break;
            }
        };

        ZoomInCommand = ReactiveCommand.Create(_chartDrawOperation.ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(_chartDrawOperation.ZoomOut);
        KeyDownCommand = ReactiveCommand.Create<KeyEventArgs>(HandleKeyDown);
    }

    private void HandleKeyDown(KeyEventArgs args) => _chartObjectManager.KeyDown(args);

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

    public ReactiveCommand<KeyEventArgs, Unit> KeyDownCommand { get; }
    
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

    private ChartMode _chartMode;
    
    public ChartMode ChartMode
    {
        get => _chartMode;
        set => SetAndRaise(ChartModeProperty, ref _chartMode, value);
    }
    
    public static readonly DirectProperty<ChartControl, ChartMode> ChartModeProperty =
        AvaloniaProperty.RegisterDirect<ChartControl, ChartMode>(
            nameof(AvailableBarsCount),
            o => o.ChartMode,
            (o, v) => o.ChartMode = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);
    
    public override void Render(DrawingContext context)
    {
        var dataShift = _data?.Length - BarsShift ?? 0;

        if (!IsPointerOver)
        {
            // When pointer has moved outside control boundaries, but PointerMoved event not fired (?).
            _chartDrawOperation.UpdatePointer(null);
        }
        
        _chartDrawOperation.BeginFrame(_data,
            dataShift,
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            _priceIndicator);

        context.Custom(_chartDrawOperation);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    public bool CrosshairVisible
    {
        get => _chartScale.CrosshairVisible;
        set => _chartScale.CrosshairVisible = value;
    }

    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
}