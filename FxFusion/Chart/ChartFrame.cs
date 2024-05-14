using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using SkiaSharp;

namespace FxFusion.Chart;

public readonly record struct ChartFrame(
    SKCanvas Canvas,
    Rect CanvasBounds,
    Rect ChartBounds,
    decimal MinPrice,
    decimal MaxPrice,
    DateTime StartDateTime,
    DateTime EndDateTime,
    IReadOnlyList<ChartSegment> Segments)
{
    public ChartSegment FindSegmentOrFail(DateTime time) =>
        Segments
            .Where(q => q.Bar.Time == time)
            .Select(q => (ChartSegment?)q)
            .SingleOrDefault() ??  throw new InvalidOperationException($"Segment for time {time} not found.");
    
    public ChartSegment FindSegmentOrFail(Point position) =>
        FindSegment(position) ??
        throw new InvalidOperationException($"Segment for position {position.ToString()} not found.");
    
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
        return (float)(ChartBounds.Height - priceDataMinDiff * PixelsPerPriceUnit() + ChartBounds.X);
    }

    public double PosYToPrice(double posY)
    {
        var yPosDiff = (float)((ChartBounds.Height + ChartBounds.Y) - posY);
        return yPosDiff / PixelsPerPriceUnit() + (double)MinPrice;
    }

    private float PixelsPerPriceUnit()
    {
        var priceRange = (double)(MaxPrice - MinPrice);
        return (float)(ChartBounds.Height / priceRange);
    }
}