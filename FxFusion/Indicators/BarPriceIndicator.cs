using FxFusion.Chart;
using SkiaSharp;

namespace FxFusion.Indicators;

public class BarPriceIndicator : IIndicator
{
    private readonly SKPaint _barPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Black,
        Style = SKPaintStyle.Stroke
    };
    
    public void Draw(in ChartFrame chartFrame, in ChartSegment chartSegment)
    {
        var segmentMiddle = chartSegment.PosX - (chartSegment.Width / 2);

        var barHighY = chartFrame.PriceToPosY(chartSegment.Bar.High);
        var barLowY = chartFrame.PriceToPosY(chartSegment.Bar.Low);
        var barOpenY = chartFrame.PriceToPosY(chartSegment.Bar.Open);
        var barCloseY = chartFrame.PriceToPosY(chartSegment.Bar.Close);
        
        var segmentMargin = chartSegment.Width * 0.1;
        
        chartFrame.Canvas.DrawLine(new SKPoint(segmentMiddle, barHighY),
            new SKPoint(segmentMiddle, barLowY),
            _barPaint);

        chartFrame.Canvas.DrawLine(new SKPoint((float)(chartSegment.PosX - chartSegment.Width + segmentMargin), barOpenY),
            new SKPoint(segmentMiddle, barOpenY),
            _barPaint);
        
        chartFrame.Canvas.DrawLine(new SKPoint(segmentMiddle, barCloseY),
            new SKPoint((float)(chartSegment.PosX - segmentMargin), barCloseY),
            _barPaint);
    }
}