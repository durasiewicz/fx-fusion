using System.Reactive;
using FxFusion.Chart;
using FxFusion.Data;
using FxFusion.Indicators;
using Microsoft.VisualBasic.CompilerServices;
using ReactiveUI;

namespace FxFusion.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public IMarketDataSource MarketDataSource { get; set; } = new StooqMarketDataSource();

    private void ShowCandles() => PriceIndicator = new CandlePriceIndicator();
    private void ShowBars() => PriceIndicator = new BarPriceIndicator();
    private void ShowLine() => PriceIndicator = new LinePriceIndicator();
    
    public MainWindowViewModel()
    {
        ShowAsCandles = ReactiveCommand.Create(ShowCandles);
        ShowAsBars = ReactiveCommand.Create(ShowBars);
        ShowAsLine = ReactiveCommand.Create(ShowLine);
    }
    
    public ReactiveCommand<Unit, Unit> ShowAsCandles { get; }
    public ReactiveCommand<Unit, Unit> ShowAsBars { get; }
    public ReactiveCommand<Unit, Unit> ShowAsLine { get; }
    
    private IIndicator _priceIndicator = new CandlePriceIndicator();
    
    public IIndicator PriceIndicator
    {
        get => _priceIndicator;
        set => this.RaiseAndSetIfChanged(ref _priceIndicator, value);
    }

    private int _barShift;
    
    public int BarShift
    {
        get => _barShift;
        set => this.RaiseAndSetIfChanged(ref _barShift, value);
    }
    
    private int _availableBarsCount;
    
    public int AvailableBarsCount
    {
        get => _availableBarsCount;
        set => this.RaiseAndSetIfChanged(ref _availableBarsCount, value);
    }
    
    private string? _selectedSymbol;
    
    public string? SelectedSymbol
    {
        get => _selectedSymbol;
        set => this.RaiseAndSetIfChanged(ref _selectedSymbol, value);
    }
    
    private string? _selectedTimeFrame;

    public string? SelectedTimeFrame
    {
        get => _selectedTimeFrame;
        set => this.RaiseAndSetIfChanged(ref _selectedTimeFrame, value);
    }
}
