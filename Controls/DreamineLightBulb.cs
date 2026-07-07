using System.Windows;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls;

/// <summary>
/// Light bulb indicator control with a shared API across WPF, WinForms, Blazor, and MAUI.
/// </summary>
public class DreamineLightBulb : FrameworkElement
{
    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(DreamineLightBulb),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(DreamineLightBulb),
            new FrameworkPropertyMetadata(96.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var d = Math.Max(32, Diameter);
        return new Size(d * 1.25, d * 1.65);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var d = Math.Max(32, Diameter);
        var cx = RenderSize.Width / 2;
        var top = 2.0;
        var glassFill = IsOn ? Color.FromRgb(255, 214, 102) : Color.FromArgb(42, 100, 116, 139);
        var glassStroke = IsOn ? Color.FromRgb(255, 196, 0) : Color.FromRgb(102, 117, 139);
        var filament = IsOn ? Color.FromRgb(122, 75, 0) : Color.FromRgb(100, 116, 139);
        var baseFill = Color.FromRgb(112, 128, 152);
        var glass = CreateGlassGeometry(cx, top, d);

        if (IsOn)
        {
            dc.PushOpacity(0.35);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 214, 102)), null, new Point(cx, top + d * .50), d * .62, d * .62);
            dc.Pop();
        }

        dc.DrawGeometry(
            new SolidColorBrush(glassFill),
            new Pen(new SolidColorBrush(glassStroke), 4),
            glass);

        var filamentGeometry = Geometry.Parse($"M {cx - d * .22:F1},{top + d * .56:F1} C {cx - d * .12:F1},{top + d * .38:F1} {cx - d * .02:F1},{top + d * .72:F1} {cx + d * .10:F1},{top + d * .53:F1} C {cx + d * .16:F1},{top + d * .44:F1} {cx + d * .21:F1},{top + d * .49:F1} {cx + d * .25:F1},{top + d * .55:F1}");
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(filament), 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, filamentGeometry);

        var neckTop = top + d * .92;
        var neck = new StreamGeometry();
        using (var ctx = neck.Open())
        {
            ctx.BeginFigure(new Point(cx - d * .30, neckTop), true, true);
            ctx.LineTo(new Point(cx + d * .30, neckTop), true, false);
            ctx.LineTo(new Point(cx + d * .20, neckTop + d * .26), true, false);
            ctx.LineTo(new Point(cx - d * .20, neckTop + d * .26), true, false);
        }
        neck.Freeze();
        dc.DrawGeometry(new SolidColorBrush(baseFill), null, neck);

        DrawBaseRib(dc, cx, neckTop + d * .10, d * .44, baseFill);
        DrawBaseRib(dc, cx, neckTop + d * .22, d * .36, baseFill);
        DrawBaseRib(dc, cx, neckTop + d * .34, d * .27, baseFill);
    }

    private static StreamGeometry CreateGlassGeometry(double cx, double top, double d)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        ctx.BeginFigure(new Point(cx, top + d * .02), true, true);
        ctx.BezierTo(new Point(cx - d * .36, top + d * .02), new Point(cx - d * .52, top + d * .27), new Point(cx - d * .52, top + d * .54), true, false);
        ctx.BezierTo(new Point(cx - d * .52, top + d * .74), new Point(cx - d * .35, top + d * .87), new Point(cx - d * .25, top + d * .96), true, false);
        ctx.LineTo(new Point(cx + d * .25, top + d * .96), true, false);
        ctx.BezierTo(new Point(cx + d * .35, top + d * .87), new Point(cx + d * .52, top + d * .74), new Point(cx + d * .52, top + d * .54), true, false);
        ctx.BezierTo(new Point(cx + d * .52, top + d * .27), new Point(cx + d * .36, top + d * .02), new Point(cx, top + d * .02), true, false);

        geometry.Freeze();
        return geometry;
    }

    private static void DrawBaseRib(DrawingContext dc, double cx, double y, double width, Color color)
    {
        var rect = new Rect(cx - width / 2, y, width, 7);
        dc.DrawRoundedRectangle(new SolidColorBrush(color), null, rect, 3.5, 3.5);
    }
}
