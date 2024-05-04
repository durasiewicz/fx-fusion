namespace FxFusion.Chart;

public interface IIndicator
{
    void Draw(in ChartFrame chartFrame, int segmentIndex);
}