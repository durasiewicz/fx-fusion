using System;
using FxFusion.Models;

namespace FxFusion.Chart;

public readonly record struct ChartSegment(
    int SegmentIndex,
    Bar Bar,
    float LeftBorderPosX,
    float RightBorderPosX)
{
    public float Width => Math.Abs(RightBorderPosX - LeftBorderPosX);
    public float Middle => RightBorderPosX - Width / 2;
}