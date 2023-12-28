using System.Collections.Generic;
using System.Threading.Tasks;
using FxFusion.Models;

namespace FxFusion.Data;

public interface IMarketDataSource<TSymbol, TTimeFrame>
{
    IEnumerable<TSymbol> AvailableSymbols { get; }
    IEnumerable<TTimeFrame> AvailableTimeFrames { get; }
    Task<Bar[]> GetData(TSymbol symbol, TTimeFrame timeFrame);
}