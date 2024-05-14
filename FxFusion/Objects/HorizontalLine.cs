using System;
using Avalonia;
using FxFusion.Chart;
using SkiaSharp;

namespace FxFusion.Objects;

public class HorizontalLine : IChartObject
{
    private bool _isHover;
    private bool _isSelected;
    private const double HoverPrecision = 5;
    
    public decimal Price { get; set; }
    
    public bool Hover(ChartFrame chartFrame, Point point, DateTime time)
    {
        _isHover = false;
        
        if (chartFrame.MinPrice > Price || chartFrame.MaxPrice < Price)
        {
            return false;
        }

        var linePointY = chartFrame.PriceToPosY(Price);

        if (point.Y >= linePointY - HoverPrecision && point.Y <= linePointY + HoverPrecision)
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
        if (chartFrame.MinPrice > Price || chartFrame.MaxPrice < Price)
        {
            return;
        }
        
        var posY = chartFrame.PriceToPosY(Price);
        chartFrame.Canvas.DrawLine(
            new SKPoint((float)chartFrame.ChartBounds.Left, posY),
            new SKPoint((float)chartFrame.ChartBounds.Right, posY), _paint);

        if (_isHover || _isSelected)
        {
            chartFrame.Canvas.DrawCircle((float)chartFrame.ChartBounds.Right / 2,
                posY,
                6,
                _paint);

            if (_isSelected)
            {
                chartFrame.Canvas.DrawCircle((float)chartFrame.ChartBounds.Right / 2,
                    posY,
                    4,
                    _selectedPaint);
            }
        }
    }
}