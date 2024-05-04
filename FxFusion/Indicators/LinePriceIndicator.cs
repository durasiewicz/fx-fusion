using System.Linq;
using FxFusion.Chart;
using SkiaSharp;

namespace FxFusion.Indicators;

public class LinePriceIndicator : IIndicator
{
    private readonly SKPaint _linePaint = new()
    {
        IsAntialias = true,
        Color = SKColors.CornflowerBlue,
        Style = SKPaintStyle.Stroke
    };
    
    public void Draw(in ChartFrame chartFrame, int segmentIndex)
    {
        if (segmentIndex >= chartFrame.Segments.Count - 1)
        {
            return;
        }
        
        var currentSegment = chartFrame.Segments[segmentIndex];
        var nextSegment = chartFrame.Segments[segmentIndex + 1];

        var currentPrice = chartFrame.PriceToPosY(currentSegment.Bar.Close);
        var nextPrice = chartFrame.PriceToPosY(nextSegment.Bar.Close);
        
        chartFrame.Canvas.DrawLine(
            new SKPoint(currentSegment.PosX, currentPrice),
            new SKPoint(nextSegment.PosX, nextPrice), _linePaint);
    }
}