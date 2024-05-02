using Avalonia;
using FxFusion.Chart;
using SkiaSharp;

namespace FxFusion.Objects;

public class HorizontalLine : IChartObject
{
    public bool IsHit(Point point)
    {
        return false;
    }

    public void Select()
    {
    }

    public void Unselect()
    {
    }

    public decimal Price { get; set; }

    public void Draw(in ChartFrame chartFrame)
    {
        if (chartFrame.MinPrice > Price || chartFrame.MaxPrice < Price)
        {
            return;
        }
        
        var posY = chartFrame.PriceToPosY(Price);
        chartFrame.Canvas.DrawLine(
            new SKPoint((float)chartFrame.ChartBounds.Left, posY),
            new SKPoint((float)chartFrame.ChartBounds.Right - chartFrame.Settings.MarginRight, posY), new SKPaint { Color = SKColors.Red });
    }
}