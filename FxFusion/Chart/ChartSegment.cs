using FxFusion.Models;

namespace FxFusion.Chart;

public record struct ChartSegment(Bar Bar,
    float PosX,
    float Width);