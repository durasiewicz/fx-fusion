using System;
using System.Linq;
using Avalonia;
using FxFusion.Chart;
using SkiaSharp;

namespace FxFusion.Objects;

public class HorizontalRay : IChartObject
{
    private bool _isHover;
    private bool _isSelected;
    private const double HoverPrecision = 5;
    
    public decimal Price { get; set; }
    public DateTime Time { get; set; }
    
    public bool Hover(ChartFrame chartFrame, Point point, DateTime time)
    {
        _isHover = false;
        
        if (time < Time || chartFrame.MinPrice > Price || chartFrame.MaxPrice < Price)
        {
            return false;
        }

        var linePointY = chartFrame.PriceToPosY(Price);

        if (point.Y >= linePointY - HoverPrecision && point.Y <= linePointY + HoverPrecision && Time <= time)
        {
            _isHover = true;
            return true;
        }
        
        return false;
    }

    public void Select()
    {
        _isSelected = true;
    }

    public void Unselect()
    {
        _isSelected = false;
    }
    
    private readonly SKPaint _paint = new SKPaint()
    {
        Color = SKColors.Black,
        StrokeWidth = 3
    };
    
    private readonly SKPaint _selectedPaint = new SKPaint()
    {
        Color = SKColors.White,
        StrokeWidth = 3
    };

    public void Draw(in ChartFrame chartFrame)
    {
        var segment = chartFrame.Segments
            .Where(q => q.Bar.Time == Time)
            .Select(q => (ChartSegment?)q)
            .SingleOrDefault();

        if (!segment.HasValue)
        {
            return;
        }
        
        var posY = chartFrame.PriceToPosY(Price);
        chartFrame.Canvas.DrawLine(
            new SKPoint(segment.Value.Middle, posY),
            new SKPoint((float)chartFrame.ChartBounds.Right - chartFrame.Settings.MarginRight, posY), _paint);

        if (_isHover || _isSelected)
        {
            chartFrame.Canvas.DrawCircle(segment.Value.Middle,
                posY,
                6,
                _paint);

            if (_isSelected)
            {
                chartFrame.Canvas.DrawCircle(segment.Value.Middle,
                    posY,
                    4,
                    _selectedPaint);
            }
        }
    }
}