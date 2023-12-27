using System;

namespace FxFusion.Models;

public record Bar(decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    DateTime Time);