using System.Collections.Generic;
using System.Threading.Tasks;
using FxFusion.Models;

namespace FxFusion.Data;

public interface IMarketDataSource
{
    IEnumerable<string> AvailableSymbols { get; }
    IEnumerable<string> AvailableTimeFrames { get; }
    Task<Bar[]> GetData(string? symbol, string? timeFrame);
}