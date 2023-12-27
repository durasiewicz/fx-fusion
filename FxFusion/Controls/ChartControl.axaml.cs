using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace FxFusion.Controls;

public partial class ChartControl : UserControl
{
    private readonly ChartDrawOperation _chartDrawOperation;

    public ChartControl()
    {
        InitializeComponent();
        ClipToBounds = true;
        _chartDrawOperation = new ChartDrawOperation();
    }

    private class ChartDrawOperation : ICustomDrawOperation
    {
        public void Dispose() { }
        public void BeginRender(Rect bounds) => Bounds = bounds;
        public Rect Bounds { get; private set; }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            
            if (leaseFeature is null)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            canvas.Clear(SKColors.Moccasin);
            canvas.Restore();
        }
    }
    
    public override void Render(DrawingContext context)
    {
        _chartDrawOperation.BeginRender(new Rect(0, 0, Bounds.Width, Bounds.Height));
        context.Custom(_chartDrawOperation);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }
}