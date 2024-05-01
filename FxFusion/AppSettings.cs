using SkiaSharp;

namespace FxFusion;

public static class AppSettings
{
    public static readonly SKPaint ChartInfoPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Black,
        Style = SKPaintStyle.Stroke,
        TextSize = 20
    };
    
    public static readonly SKPaint ScaleLabelTextPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.White,
        FilterQuality = SKFilterQuality.High,
    };
    
    public static readonly SKPaint ScaleTextPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Black,
        FilterQuality = SKFilterQuality.High,
    };
    
    public static readonly SKPaint ScaleBorderPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Black,
        Style = SKPaintStyle.Fill,
        FilterQuality = SKFilterQuality.High,
    };
}