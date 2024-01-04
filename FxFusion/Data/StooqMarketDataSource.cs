using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FxFusion.Models;

namespace FxFusion.Data;

public class StooqMarketDataSource : IMarketDataSource
{
    public IEnumerable<string> AvailableSymbols => new[]
    {
        "PKO",
        "XTB"
    };

    public IEnumerable<string> AvailableTimeFrames => new[]
    {
        "D",
        "W",
        "M"
    };

    public async Task<Bar[]> GetData(string? symbol, string? timeFrame)
    {
        if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(timeFrame))
        {
            return Array.Empty<Bar>();
        }
        
        if (!Directory.Exists("data"))
        {
            Directory.CreateDirectory("data");
        }
    
        var normalizedSymbol = symbol.Replace("/", "").ToLower();
        var normalizedTimeFrame = timeFrame.Replace("/", "").ToLower();
        var dataFiles = Directory.GetFiles("data");
        var currentDataFileName = $"{normalizedSymbol}-{normalizedTimeFrame}-{DateTime.Now:yyyy-MM-dd}.csv";
        var filePath = $"data/{currentDataFileName}";
        var dataFile = dataFiles
            .SingleOrDefault(q => Path.GetFileName(q).Trim().ToUpperInvariant() == currentDataFileName.Trim().ToUpperInvariant());

        if (dataFile is null)
        {
           await DownloadFileAsync($"https://stooq.pl/q/d/l/?s={normalizedSymbol}&i={normalizedTimeFrame}", filePath);
        }

        var data = await ReadFile(filePath);
        
        return data.ToArray();

        // Latest data needs to be on lowest index thus we reverse enumeration
        Task<IEnumerable<Bar>> ReadFile(string fileName) => Task.Run(() =>
            (from cells in (from line in File.ReadAllLines(fileName).Skip(1) select line.Split(','))
                let decimalCulture = new CultureInfo("en-US")
                select new Bar(decimal.Parse(cells[1], decimalCulture),
                    decimal.Parse(cells[2], decimalCulture),
                    decimal.Parse(cells[3], decimalCulture),
                    decimal.Parse(cells[4], decimalCulture),
                    DateTime.Parse(cells[0]))).Reverse());
    }
    
    private async Task DownloadFileAsync(string url, string destinationPath)
    {
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await using (
            Stream contentStream = await (await httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
            stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 3145728, true))
        {
            await contentStream.CopyToAsync(stream);
        }
    }
    
}