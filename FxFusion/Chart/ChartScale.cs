using System;
using System.Globalization;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using SkiaSharp;

namespace FxFusion.Chart;

public class ChartScale
{
    public bool CrosshairVisible { get; set; }

    public void Draw(in ChartFrame chartFrame, Point? pointerPosition)
    {
        DrawXScale(in chartFrame, pointerPosition);
        DrawYScale(in chartFrame, pointerPosition);
    }

    public Rect AdjustChartBounds(Rect chartBounds) =>
        new Rect(chartBounds.X, chartBounds.Y, chartBounds.Width - ScaleYWidth, chartBounds.Height - ScaleXHeight - 10);

    private void DrawXScale(in ChartFrame chartFrame, Point? pointerPosition)
    {
        var timeLabelFormattedText = new FormattedText(DateTime.Now.ToString("yyyy-MM-dd"),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            AppSettings.ScaleLabelTextPaint.TextSize,
            null);
        
        var posY = (float)(chartFrame.CanvasBounds.Y + chartFrame.CanvasBounds.Height - ScaleXHeight);

        chartFrame.Canvas.DrawLine(new SKPoint((float)chartFrame.CanvasBounds.X, posY),
            new SKPoint((float)(chartFrame.CanvasBounds.X + chartFrame.CanvasBounds.Width), posY),
            AppSettings.ScaleBorderPaint);

        var lastTimeLabelPosX = chartFrame.CanvasBounds.X + chartFrame.CanvasBounds.Width;
        var timeLabelPosY = (float)(chartFrame.CanvasBounds.Y + chartFrame.CanvasBounds.Height - 5);

        foreach (var chartSegment in chartFrame.Segments)
        {
            if (chartSegment.LeftBorderPosX + timeLabelFormattedText.Width < lastTimeLabelPosX - 10)
            {
                chartFrame.Canvas.DrawText(chartSegment.Bar.Time.ToString("yyyy-MM-dd"),
                    chartSegment.LeftBorderPosX,
                    timeLabelPosY,
                    AppSettings.ScaleTextPaint);
            
                lastTimeLabelPosX = chartSegment.LeftBorderPosX;
            }
        }

        ChartSegment? hoveredSegment = null;

        if (CrosshairVisible && pointerPosition.HasValue)
        {
            hoveredSegment = chartFrame.FindSegment(pointerPosition.Value);
        }

        if (!hoveredSegment.HasValue)
        {
            return;
        }
        
        var (posX, time) = (hoveredSegment.Value.Middle, hoveredSegment.Value.Bar.Time);

        var timeLabelText = time.ToString("yyyy-MM-dd");
        var defaultTypeface = new Typeface(FontFamily.Default);

        var formattedText = new FormattedText(timeLabelText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            defaultTypeface,
            AppSettings.ScaleLabelTextPaint.TextSize,
            null);

        var textHalfWidth = (float)(formattedText.Width / 2);
        var leftRightPadding = 10f;

        chartFrame.Canvas.DrawRect(new SKRect(posX - textHalfWidth - leftRightPadding,
                posY,
                posX + textHalfWidth + leftRightPadding,
                (float)(chartFrame.CanvasBounds.Y + chartFrame.CanvasBounds.Height)),
            AppSettings.ScaleBorderPaint);

        chartFrame.Canvas.DrawText(timeLabelText,
            posX - textHalfWidth,
            timeLabelPosY,
            AppSettings.ScaleLabelTextPaint);
            
        chartFrame.Canvas.DrawLine(new SKPoint(posX, 0),
            new SKPoint(posX, (float)(chartFrame.CanvasBounds.Y + chartFrame.CanvasBounds.Height)),
            AppSettings.ScaleBorderPaint);
    }

    private const int ScaleYWidth = 50;
    private const int ScaleXHeight = 20;

    private void DrawYScale(in ChartFrame chartFrame, Point? pointerPosition)
    {
        var scaleYStep = CalculateYScaleStep(in chartFrame);
        var scaleYMax = Math.Floor((float)chartFrame.MaxPrice / scaleYStep) * scaleYStep;
        var scaleYMin = Math.Floor((float)chartFrame.MinPrice / scaleYStep) * scaleYStep;
        var currentPrice = scaleYMax;

        var scaleBorderX = (float)(chartFrame.CanvasBounds.X + chartFrame.CanvasBounds.Width) - 0.5f;
        var scaleBottomY = (float)(chartFrame.CanvasBounds.Y + chartFrame.CanvasBounds.Height - ScaleXHeight);

        chartFrame.Canvas.DrawLine(new SKPoint(scaleBorderX - ScaleYWidth, (float)chartFrame.CanvasBounds.Y),
            new SKPoint(scaleBorderX - ScaleYWidth, scaleBottomY),
            AppSettings.ScaleBorderPaint);

        while (currentPrice >= scaleYMin)
        {
            var posY = chartFrame.PriceToPosY((decimal)currentPrice);

            if (posY >= scaleBottomY)
            {
                break;
            }

            chartFrame.Canvas.DrawLine(new SKPoint(scaleBorderX - ScaleYWidth, posY),
                new SKPoint(scaleBorderX - ScaleYWidth + 5, posY),
                AppSettings.ScaleBorderPaint);

            chartFrame.Canvas.DrawText(currentPrice.ToString("0.##"),
                ((float)scaleBorderX - 25),
                posY,
                AppSettings.ScaleBorderPaint);

            currentPrice -= scaleYStep;
        }

        if (CrosshairVisible && pointerPosition.HasValue)
        {
            chartFrame.Canvas.DrawRect(new SKRect(scaleBorderX - ScaleYWidth,
                (float)(pointerPosition.Value.Y - 10.5f),
                (float)(chartFrame.CanvasBounds.X + chartFrame.CanvasBounds.Width),
                (float)(pointerPosition.Value.Y + 10.5f)), AppSettings.ScaleBorderPaint);

            chartFrame.Canvas.DrawText(chartFrame.PosYToPrice(pointerPosition.Value.Y).ToString("0.##"),
                scaleBorderX - ScaleYWidth + 10,
                (float)pointerPosition.Value.Y + 5,
                AppSettings.ScaleLabelTextPaint);
            
            chartFrame.Canvas.DrawLine(new SKPoint(0, (float)pointerPosition.Value.Y - 0.5f),
                new SKPoint((float)(chartFrame.CanvasBounds.X + chartFrame.CanvasBounds.Width - ScaleYWidth), (float)pointerPosition.Value.Y - 0.5f),
                AppSettings.ScaleBorderPaint);
        }
    }

    private double CalculateYScaleStep(in ChartFrame chartFrame)
    {
        var visibleSteps = chartFrame.CanvasBounds.Height / 20;
        var roughStep = (double)chartFrame.MaxPrice / (visibleSteps - 1);
        var exponent = Math.Floor(Math.Log10(roughStep));
        var magnitude = Math.Pow(10, exponent);
        var fraction = roughStep / magnitude;

        fraction = fraction switch
        {
            < 2 => 1,
            < 3 => 2,
            < 6 => 3,
            < 7 => 5,
            < 8 => 6,
            _ => 10
        };

        return fraction * Math.Pow(10, exponent);
    }
}