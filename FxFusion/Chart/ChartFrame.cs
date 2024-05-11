using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using SkiaSharp;

namespace FxFusion.Chart;

public readonly record struct ChartFrame(
    SKCanvas Canvas,
    Rect ChartBounds,
    ChartSettings Settings,
    decimal MinPrice,
    decimal MaxPrice,
    DateTime StartDateTime,
    DateTime EndDateTime,
    IReadOnlyList<ChartSegment> Segments)
{
    public ChartSegment? FindSegment(Point position) =>
        Segments
            // Lookup open interval, because segments shares same X position as border
            .Where(q => q.LeftBorderPosX < position.X)
            .Where(q => q.RightBorderPosX > position.X)
            .Select(q => (ChartSegment?)q)
            .SingleOrDefault();

    public float PriceToPosY(decimal price)
    {
        var priceDataMinDiff = (float)(price - MinPrice);
        return (float)(ChartBounds.Height - Settings.MarginTop - Settings.MarginBottom -
                       priceDataMinDiff * PixelsPerPriceUnit()) + (Settings.MarginTop >> 1);
    }

    public double PosYToPrice(double posY)
    {
        var yPosDiff = (float)(ChartBounds.Height - Settings.MarginTop - Settings.MarginBottom - posY) +
                       (Settings.MarginTop >> 1);
        
        return yPosDiff / PixelsPerPriceUnit() + (double)MinPrice;
    }

    private float PixelsPerPriceUnit()
    {
        var priceRange = (double)(MaxPrice - MinPrice);
        
        return (float)((ChartBounds.Height - Settings.MarginTop - Settings.MarginBottom) / priceRange);
    }
}