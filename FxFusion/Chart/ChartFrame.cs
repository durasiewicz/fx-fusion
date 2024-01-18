using Avalonia;
using SkiaSharp;

namespace FxFusion.Chart;

public readonly record struct ChartFrame(
    SKCanvas Canvas,
    Rect ChartBounds,
    ChartSettings Settings,
    decimal MinPrice,
    decimal MaxPrice)
{
    public float PriceToPosY(decimal price)
    {
        var priceDataMinDiff = (float)(price - MinPrice);
        return (float)(ChartBounds.Height - Settings.MarginTop - Settings.MarginBottom -
                       priceDataMinDiff * PixelsPerPriceUnit()) + (Settings.MarginTop >> 1);
    }

    public double PosYToPrice(double posY)
    {
        var priceDataMinDiff = (ChartBounds.Height - Settings.MarginTop - posY) / PixelsPerPriceUnit();
        return (double)MinPrice + priceDataMinDiff;
    }

    private float PixelsPerPriceUnit()
    {
        var priceRange = (double)(MaxPrice - MinPrice);
        return (float)((ChartBounds.Height - Settings.MarginTop - Settings.MarginBottom) / priceRange);
    }
}