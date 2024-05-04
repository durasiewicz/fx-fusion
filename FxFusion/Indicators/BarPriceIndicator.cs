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
    
    public void Draw(in ChartFrame chartFrame, int segmentIndex)
    {
        var chartSegment = chartFrame.Segments[segmentIndex];
        var barHighY = chartFrame.PriceToPosY(chartSegment.Bar.High);
        var barLowY = chartFrame.PriceToPosY(chartSegment.Bar.Low);
        var barOpenY = chartFrame.PriceToPosY(chartSegment.Bar.Open);
        var barCloseY = chartFrame.PriceToPosY(chartSegment.Bar.Close);
        
        var segmentMargin = chartSegment.Width * 0.1;
        
        chartFrame.Canvas.DrawLine(new SKPoint(chartSegment.Middle, barHighY),
            new SKPoint(chartSegment.Middle, barLowY),
            _barPaint);

        chartFrame.Canvas.DrawLine(new SKPoint((float)(chartSegment.LeftBorderPosX + segmentMargin), barOpenY),
            new SKPoint(chartSegment.Middle, barOpenY),
            _barPaint);
        
        chartFrame.Canvas.DrawLine(new SKPoint(chartSegment.Middle, barCloseY),
            new SKPoint((float)(chartSegment.RightBorderPosX - segmentMargin), barCloseY),
            _barPaint);
    }
}