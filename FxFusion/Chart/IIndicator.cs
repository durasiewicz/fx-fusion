namespace FxFusion.Chart;

public interface IIndicator
{
    void Draw(in ChartFrame chartFrame, in ChartSegment chartSegment);
}