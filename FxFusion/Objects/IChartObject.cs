using Avalonia;
using FxFusion.Chart;

namespace FxFusion.Objects;

public interface IChartObject
{
    bool IsHit(Point point);
    void Select();
    void Unselect();
    void Draw(in ChartFrame chartFrame);
}