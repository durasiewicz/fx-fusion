using FxFusion.Data;
using ReactiveUI;

namespace FxFusion.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public IMarketDataSource MarketDataSource { get; set; } = new StooqMarketDataSource();

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
