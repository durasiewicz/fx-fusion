﻿using System;
using FxFusion.Chart;
using SkiaSharp;

namespace FxFusion.Indicators;

public class CandlePriceIndicator : IIndicator
{
    private readonly SKPaint _barPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Black,
        Style = SKPaintStyle.Stroke
    };
    
    private readonly SKPaint _bullCandlePaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Green,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _bearCandlePaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Crimson,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _candleBodyBorder = new()
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
        var segmentMargin = chartSegment.Width * 0.1;
        
        chartFrame.Canvas.DrawLine(new SKPoint(chartSegment.Middle, barHighY),
            new SKPoint(chartSegment.Middle, barLowY),
            _barPaint);

        var bodyRect = new SKRect((float)(chartSegment.LeftBorderPosX + segmentMargin),
            chartFrame.PriceToPosY(Math.Max(chartSegment.Bar.Open, chartSegment.Bar.Close)),
            (float)(chartSegment.RightBorderPosX - segmentMargin),
            chartFrame.PriceToPosY(Math.Min(chartSegment.Bar.Open, chartSegment.Bar.Close)));

        chartFrame.Canvas.DrawRect(bodyRect, chartSegment.Bar.Open > chartSegment.Bar.Close ? _bearCandlePaint : _bullCandlePaint);
        chartFrame.Canvas.DrawRect(bodyRect, _candleBodyBorder);
    }
}