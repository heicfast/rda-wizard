// Arc shape — the circular-gauge ring segment. A custom FrameworkElement-derived Shape
// with StartAngle/Sweep (degrees, 0 = 12 o'clock, clockwise) rendered via arc geometry.
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RdaWizard.Controls;

public class Arc : Shape
{
    public static readonly DependencyProperty StartAngleProperty =
        DependencyProperty.Register(nameof(StartAngle), typeof(double), typeof(Arc),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SweepProperty =
        DependencyProperty.Register(nameof(Sweep), typeof(double), typeof(Arc),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double StartAngle
    {
        get => (double)GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    public double Sweep
    {
        get => (double)GetValue(SweepProperty);
        set => SetValue(SweepProperty, value);
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            var size = new Size(Math.Max(0, RenderSize.Width - StrokeThickness),
                                Math.Max(0, RenderSize.Height - StrokeThickness));
            if (size.Width < 1 || size.Height < 1 || Sweep <= 0)
            {
                return new StreamGeometry();
            }
            var radius = Math.Min(size.Width, size.Height) / 2;
            var center = new Point(size.Width / 2, size.Height / 2);
            // 0° at 12 o'clock, clockwise: Qt-style gauge origin (EuCircleProgress parity).
            var start = ToCartesian(center, radius, StartAngle);
            var end = ToCartesian(center, radius, StartAngle + Sweep);
            var large = Sweep > 180;
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(start, false, false);
                ctx.ArcTo(end, new Size(radius, radius), 0, large, SweepDirection.Clockwise, true, false);
            }
            geo.Freeze();
            return geo;
        }
    }

    private static Point ToCartesian(Point center, double radius, double angleDeg)
    {
        var rad = (angleDeg - 90) * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
    }
}
