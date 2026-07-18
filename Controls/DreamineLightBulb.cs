using System.Windows;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls;

/// <summary>
/// \if KO
/// <para>여러 UI 플랫폼과 공통 API를 사용하는 전구 상태 표시 WPF 요소입니다.</para>
/// \endif
/// \if EN
/// <para>Provides a WPF light-bulb indicator element with an API shared across UI platforms.</para>
/// \endif
/// </summary>
public class DreamineLightBulb : FrameworkElement
{
    /// <summary>
    /// \if KO
    /// <para>전구의 켜짐 상태를 저장하는 종속성 속성입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the dependency property that stores the bulb's on state.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(DreamineLightBulb),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// \if KO
    /// <para>전구 지름을 저장하는 종속성 속성입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the dependency property that stores the bulb diameter.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(DreamineLightBulb),
            new FrameworkPropertyMetadata(96.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// \if KO
    /// <para>전구가 켜져 있는지 여부를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether the bulb is on.</para>
    /// \endif
    /// </summary>
    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    /// <summary>
    /// \if KO
    /// <para>전구 본체의 기준 지름을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the reference diameter of the bulb body.</para>
    /// \endif
    /// </summary>
    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    /// <summary>
    /// \if KO
    /// <para>최소 지름을 적용하여 전구 전체가 들어갈 원하는 크기를 계산합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Calculates the desired size that contains the bulb after applying its minimum diameter.</para>
    /// \endif
    /// </summary>
    /// <param name="availableSize">
    /// \if KO
    /// <para>부모 요소가 제공하는 사용 가능 크기입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The available size supplied by the parent.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>전구 렌더링에 필요한 크기입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The size required to render the bulb.</para>
    /// \endif
    /// </returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        var d = Math.Max(32, Diameter);
        return new Size(d * 1.25, d * 1.65);
    }

    /// <summary>
    /// \if KO
    /// <para>현재 상태에 맞는 빛, 유리, 필라멘트와 전구 베이스를 그립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Draws the glow, glass, filament, and bulb base for the current state.</para>
    /// \endif
    /// </summary>
    /// <param name="dc">
    /// \if KO
    /// <para>그리기 명령을 받을 렌더링 컨텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The drawing context that receives rendering commands.</para>
    /// \endif
    /// </param>
    /// <exception cref="FormatException">
    /// \if KO
    /// <para>계산된 필라멘트 경로 문자열을 기하 도형으로 해석할 수 없으면 발생할 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>May be thrown when the calculated filament path text cannot be parsed as geometry.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>전구 유리 외곽선을 나타내는 고정된 스트림 기하 도형을 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates a frozen stream geometry representing the bulb's glass outline.</para>
    /// \endif
    /// </summary>
    /// <param name="cx">
    /// \if KO
    /// <para>전구 중심의 X 좌표입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bulb center's X coordinate.</para>
    /// \endif
    /// </param>
    /// <param name="top">
    /// \if KO
    /// <para>전구 상단의 Y 좌표입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bulb top's Y coordinate.</para>
    /// \endif
    /// </param>
    /// <param name="d">
    /// \if KO
    /// <para>전구 기준 지름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The bulb reference diameter.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>고정된 유리 외곽선 기하 도형입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The frozen glass-outline geometry.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>전구 베이스의 둥근 가로 리브 하나를 그립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Draws one rounded horizontal rib on the bulb base.</para>
    /// \endif
    /// </summary>
    /// <param name="dc">
    /// \if KO
    /// <para>그리기 컨텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The drawing context.</para>
    /// \endif
    /// </param>
    /// <param name="cx">
    /// \if KO
    /// <para>리브 중심의 X 좌표입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The rib center's X coordinate.</para>
    /// \endif
    /// </param>
    /// <param name="y">
    /// \if KO
    /// <para>리브 상단의 Y 좌표입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The rib top's Y coordinate.</para>
    /// \endif
    /// </param>
    /// <param name="width">
    /// \if KO
    /// <para>리브 너비입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The rib width.</para>
    /// \endif
    /// </param>
    /// <param name="color">
    /// \if KO
    /// <para>리브 채우기 색입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The rib fill color.</para>
    /// \endif
    /// </param>
    private static void DrawBaseRib(DrawingContext dc, double cx, double y, double width, Color color)
    {
        var rect = new Rect(cx - width / 2, y, width, 7);
        dc.DrawRoundedRectangle(new SolidColorBrush(color), null, rect, 3.5, 3.5);
    }
}
