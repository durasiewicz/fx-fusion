using FxFusion.Data;

namespace FxFusion.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public IMarketDataSource MarketDataSource { get; set; } = new StooqMarketDataSource();
}
