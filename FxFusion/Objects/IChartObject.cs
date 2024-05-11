using System;
using Avalonia;
using FxFusion.Chart;

namespace FxFusion.Objects;

public interface IChartObject
{
    bool Hover(ChartFrame chartFrame, Point point, DateTime time);
    void Select();
    void Unselect();
    void Draw(in ChartFrame chartFrame);
}