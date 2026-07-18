using Microsoft.Xaml.Behaviors.Layout;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>외곽 링과 내부 코어를 요소 모서리에 표시하고 상태·펄스 애니메이션을 지원하는 경량 LED 컨트롤입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a lightweight corner LED with an outer ring, inner core, state brushes, and pulse animation.</para>
	/// \endif
	/// </summary>
	public class DreamineCheckLed : Control
	{
		/// <summary>
		/// \if KO
		/// <para>켜짐 상태의 기본 외곽 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the default outer brush for the on state.</para>
		/// \endif
		/// </summary>
		private static readonly Brush _onOuter = Freeze(CreateRadial(
			new[] { ("#FF1FD36B", 0.00), ("#FFA7F0C1", 0.60), ("#00FFFFFF", 1.00) }, 0.6, 0.6));
		/// <summary>
		/// \if KO
		/// <para>켜짐 상태의 기본 내부 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the default inner brush for the on state.</para>
		/// \endif
		/// </summary>
		private static readonly Brush _onInner = Freeze(CreateRadial(
			new[] { ("#FF6E6E6E", 0.00), ("#FF4B4B4B", 1.00) }, 0.65, 0.65));
		// Off 상태는 "링"만 보여야 하므로 Fill은 투명으로 둡니다.
		/// <summary>
		/// \if KO
		/// <para>꺼짐 상태의 기본 외곽 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the default outer brush for the off state.</para>
		/// \endif
		/// </summary>
		private static readonly Brush _offOuter = Brushes.Transparent;
		/// <summary>
		/// \if KO
		/// <para>꺼짐 상태의 기본 내부 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the default inner brush for the off state.</para>
		/// \endif
		/// </summary>
		private static readonly Brush _offInner = Brushes.Transparent;

		/// <summary>
		/// \if KO
		/// <para>문자열 색상 정지점과 반지름으로 고정된 방사형 그라데이션 브러시를 만듭니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates a frozen radial-gradient brush from string color stops and radii.</para>
		/// \endif
		/// </summary>
		/// <param name="stops">
		/// \if KO
		/// <para>색상 문자열과 오프셋 배열입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Color-string and offset pairs.</para>
		/// \endif
		/// </param>
		/// <param name="rx">
		/// \if KO
		/// <para>가로 반지름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The horizontal radius.</para>
		/// \endif
		/// </param>
		/// <param name="ry">
		/// \if KO
		/// <para>세로 반지름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The vertical radius.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>생성된 방사형 그라데이션 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The created radial-gradient brush.</para>
		/// \endif
		/// </returns>
		/// <exception cref="FormatException">
		/// \if KO
		/// <para>색상 문자열을 해석할 수 없으면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when a color string cannot be parsed.</para>
		/// \endif
		/// </exception>
		private static RadialGradientBrush CreateRadial((string color, double offset)[] stops, double rx, double ry)
		{
			var b = new RadialGradientBrush
			{
				Center = new Point(0.5, 0.5),
				GradientOrigin = new Point(0.5, 0.5),
				RadiusX = rx,
				RadiusY = ry
			};
			foreach (var (c, off) in stops)
				b.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(c), off));
			if (b.CanFreeze) b.Freeze();
			return b;
		}
		/// <summary>
		/// \if KO
		/// <para>가능하면 브러시를 고정하고 같은 인스턴스를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Freezes a brush when possible and returns the same instance.</para>
		/// \endif
		/// </summary>
		/// <param name="b">
		/// \if KO
		/// <para>고정할 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The brush to freeze.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>입력 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The input brush.</para>
		/// \endif
		/// </returns>
		private static Brush Freeze(Brush b) { if (b.CanFreeze) b.Freeze(); return b; }

		/// <summary>
		/// \if KO
		/// <para>기본 스타일과 비상호작용 메타데이터를 설정하고 LED 테마 리소스를 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Configures default styling and noninteractive metadata, then merges LED theme resources.</para>
		/// \endif
		/// </summary>
		static DreamineCheckLed()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(DreamineCheckLed),
				new FrameworkPropertyMetadata(typeof(DreamineCheckLed)));

			try
			{
				// ⬇️ HitTest 기본값을 false로 고정
				IsHitTestVisibleProperty.OverrideMetadata(
					typeof(DreamineCheckLed),
					new FrameworkPropertyMetadata(false));

				// (권장) 포커스/탭정지도 기본 비활성화
				FocusableProperty.OverrideMetadata(
					typeof(DreamineCheckLed),
					new FrameworkPropertyMetadata(false));
				IsTabStopProperty.OverrideMetadata(
					typeof(DreamineCheckLed),
					new FrameworkPropertyMetadata(false));

				var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineCheckLedStyle.xaml",
									UriKind.RelativeOrAbsolute);
				var app = Application.Current;
				if (app != null)
				{
					bool alreadyAdded = app.Resources.MergedDictionaries
						.OfType<ResourceDictionary>()
						.Any(x => x.Source != null && x.Source.Equals(uri));

					if (!alreadyAdded)
					{
						var dict = new ResourceDictionary { Source = uri };
						app.Resources.MergedDictionaries.Add(dict);
					}
				}
			}
			catch
			{
				// Guard for design-time or missing resource scenarios.
			}
		}


		/// <summary>
		/// \if KO
		/// <para>LED 켜짐 상태 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the LED on-state dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(DreamineCheckLed),
				new PropertyMetadata(false, OnPulseStateChanged));

		/// <summary>
		/// \if KO
		/// <para>LED 펄스 애니메이션 사용 여부 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the LED pulse-animation dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsPulseProperty =
			DependencyProperty.Register(nameof(IsPulse), typeof(bool), typeof(DreamineCheckLed),
				new PropertyMetadata(false, OnPulseStateChanged));

		/// <summary>
		/// \if KO
		/// <para>켜짐과 펄스가 모두 활성화되면 불투명도 반복 애니메이션을 시작하고 아니면 중지합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Starts repeating opacity animation when both on and pulse states are active; otherwise, stops it.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>상태가 변경된 LED입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The LED whose state changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>종속성 속성 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency-property change data.</para>
		/// \endif
		/// </param>
		private static void OnPulseStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not DreamineCheckLed led) return;

			if (led.IsOn && led.IsPulse)
			{
				var pulse = new DoubleAnimation
				{
					From = 0.15,
					To = 1.0,
					Duration = TimeSpan.FromSeconds(0.4),
					AutoReverse = true,
					RepeatBehavior = RepeatBehavior.Forever
				};
				led.BeginAnimation(OpacityProperty, pulse);
			}
			else
			{
				led.BeginAnimation(OpacityProperty, null);
				led.Opacity = 1.0;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>LED 표시 모서리 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the LED display-corner dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CornerProperty =
			DependencyProperty.Register(nameof(Corner), typeof(LedCorner), typeof(DreamineCheckLed),
				new PropertyMetadata(LedCorner.TopRight));

		/// <summary>
		/// \if KO
		/// <para>LED 지름 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the LED diameter dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty DiameterProperty =
			DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(18.0));

		/// <summary>
		/// \if KO
		/// <para>가장자리 오프셋 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the edge-offset dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty EdgeOffsetProperty =
			DependencyProperty.Register(nameof(EdgeOffset), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(6.0));

		/// <summary>
		/// \if KO
		/// <para>내부 원 지름 비율 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the inner-circle diameter-scale dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty InnerScaleProperty =
			DependencyProperty.Register(nameof(InnerScale), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(0.45));

		/// <summary>
		/// \if KO
		/// <para>켜짐 외곽 채우기 브러시 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the on-state outer-fill brush dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OnOuterFillProperty =
			DependencyProperty.Register(nameof(OnOuterFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_onOuter));
		/// <summary>
		/// \if KO
		/// <para>켜짐 내부 채우기 브러시 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the on-state inner-fill brush dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OnInnerFillProperty =
			DependencyProperty.Register(nameof(OnInnerFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_onInner));
		/// <summary>
		/// \if KO
		/// <para>꺼짐 외곽 채우기 브러시 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the off-state outer-fill brush dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OffOuterFillProperty =
			DependencyProperty.Register(nameof(OffOuterFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_offOuter));
		/// <summary>
		/// \if KO
		/// <para>꺼짐 내부 채우기 브러시 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the off-state inner-fill brush dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OffInnerFillProperty =
			DependencyProperty.Register(nameof(OffInnerFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_offInner));

		/// <summary>
		/// \if KO
		/// <para>외곽 링 선 브러시 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the outer-ring stroke-brush dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty StrokeBrushProperty =
			DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(Brushes.White));
		/// <summary>
		/// \if KO
		/// <para>외곽 링 선 두께 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the outer-ring stroke-thickness dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty StrokeThicknessProperty =
			DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(1.2));

		/// <summary>
		/// \if KO
		/// <para>LED가 켜져 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the LED is on.</para>
		/// \endif
		/// </summary>
		public bool IsOn { get => (bool)GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>펄스 애니메이션을 사용할지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether pulse animation is enabled.</para>
		/// \endif
		/// </summary>
		public bool IsPulse { get => (bool)GetValue(IsPulseProperty); set => SetValue(IsPulseProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>LED를 표시할 모서리를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the corner at which the LED is displayed.</para>
		/// \endif
		/// </summary>
		public LedCorner Corner { get => (LedCorner)GetValue(CornerProperty); set => SetValue(CornerProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>LED 지름을 픽셀 단위로 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the LED diameter in pixels.</para>
		/// \endif
		/// </summary>
		public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>가장자리 여백을 픽셀 단위로 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the edge offset in pixels.</para>
		/// \endif
		/// </summary>
		public double EdgeOffset { get => (double)GetValue(EdgeOffsetProperty); set => SetValue(EdgeOffsetProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>내부 원 지름 비율을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the inner-circle diameter scale.</para>
		/// \endif
		/// </summary>
		public double InnerScale { get => (double)GetValue(InnerScaleProperty); set => SetValue(InnerScaleProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>켜짐 상태의 외곽 채우기 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the on-state outer-fill brush.</para>
		/// \endif
		/// </summary>
		public Brush OnOuterFill { get => (Brush)GetValue(OnOuterFillProperty); set => SetValue(OnOuterFillProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>켜짐 상태의 내부 채우기 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the on-state inner-fill brush.</para>
		/// \endif
		/// </summary>
		public Brush OnInnerFill { get => (Brush)GetValue(OnInnerFillProperty); set => SetValue(OnInnerFillProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>꺼짐 상태의 외곽 채우기 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the off-state outer-fill brush.</para>
		/// \endif
		/// </summary>
		public Brush OffOuterFill { get => (Brush)GetValue(OffOuterFillProperty); set => SetValue(OffOuterFillProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>꺼짐 상태의 내부 채우기 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the off-state inner-fill brush.</para>
		/// \endif
		/// </summary>
		public Brush OffInnerFill { get => (Brush)GetValue(OffInnerFillProperty); set => SetValue(OffInnerFillProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>외곽 링 선 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the outer-ring stroke brush.</para>
		/// \endif
		/// </summary>
		public Brush StrokeBrush { get => (Brush)GetValue(StrokeBrushProperty); set => SetValue(StrokeBrushProperty, value); }
		/// <summary>
		/// \if KO
		/// <para>외곽 링 선 두께를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the outer-ring stroke thickness.</para>
		/// \endif
		/// </summary>
		public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
	}



	/// <summary>
	/// \if KO
	/// <para>임의의 WPF 요소 위에 <see cref="DreamineCheckLed"/>를 Adorner로 표시하는 연결 속성을 제공합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides attached properties that display a <see cref="DreamineCheckLed"/> as an adorner over any WPF element.</para>
	/// \endif
	/// </summary>
	public static class DreamineCheckLedAttach
	{
		#region Public Attached DPs

		/// <summary>
		/// \if KO
		/// <para>LED Adorner 활성화 여부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached property that enables the LED adorner.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(false, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>LED 켜짐 상태 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached LED on-state property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.RegisterAttached("IsOn", typeof(bool), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(false, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>펄스 애니메이션 사용 여부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached pulse-animation property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsPulseProperty =
			DependencyProperty.RegisterAttached("IsPulse", typeof(bool), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(false, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>LED 표시 모서리 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached LED display-corner property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CornerProperty =
			DependencyProperty.RegisterAttached("Corner", typeof(LedCorner), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(LedCorner.TopRight, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>LED 지름 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached LED diameter property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty DiameterProperty =
			DependencyProperty.RegisterAttached("Diameter", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(18.0, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>픽셀 단위 가장자리 여백 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached edge-margin property in pixels.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty MarginProperty =
			DependencyProperty.RegisterAttached("Margin", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(6.0, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>내부 원 지름 비율 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached inner-circle scale property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty InnerScaleProperty =
			DependencyProperty.RegisterAttached("InnerScale", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(0.45, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>켜짐 외곽 채우기 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached on-state outer-fill property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OnOuterFillProperty =
			DependencyProperty.RegisterAttached("OnOuterFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));
		/// <summary>
		/// \if KO
		/// <para>켜짐 내부 채우기 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached on-state inner-fill property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OnInnerFillProperty =
			DependencyProperty.RegisterAttached("OnInnerFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));
		/// <summary>
		/// \if KO
		/// <para>꺼짐 외곽 채우기 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached off-state outer-fill property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OffOuterFillProperty =
			DependencyProperty.RegisterAttached("OffOuterFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));
		/// <summary>
		/// \if KO
		/// <para>꺼짐 내부 채우기 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached off-state inner-fill property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OffInnerFillProperty =
			DependencyProperty.RegisterAttached("OffInnerFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));

		/// <summary>
		/// \if KO
		/// <para>외곽 링 선 브러시 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached outer-ring stroke-brush property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty StrokeBrushProperty =
			DependencyProperty.RegisterAttached("StrokeBrush", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(Brushes.White, OnAnyPropertyChanged));
		/// <summary>
		/// \if KO
		/// <para>외곽 링 선 두께 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached outer-ring stroke-thickness property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty StrokeThicknessProperty =
			DependencyProperty.RegisterAttached("StrokeThickness", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(1.2, OnAnyPropertyChanged));

		// Get/Set
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED Adorner 활성화 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether the LED adorner is enabled on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>활성화 여부입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Whether the adorner is enabled.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetIsEnabled(DependencyObject o, bool v) => o.SetValue(IsEnabledProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED Adorner 활성화 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether the LED adorner is enabled on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>활성화되어 있으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when enabled.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static bool GetIsEnabled(DependencyObject o) => (bool)o.GetValue(IsEnabledProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 켜짐 상태를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the LED on state on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>켜짐 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The on state.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetIsOn(DependencyObject o, bool v) => o.SetValue(IsOnProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 켜짐 상태를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the LED on state from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>켜져 있으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when on.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static bool GetIsOn(DependencyObject o) => (bool)o.GetValue(IsOnProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 펄스 애니메이션 사용 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether pulse animation is enabled on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>펄스 사용 여부입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Whether pulse is enabled.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetIsPulse(DependencyObject o, bool v) => o.SetValue(IsPulseProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 펄스 애니메이션 사용 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether pulse animation is enabled on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>사용하면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when enabled.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static bool GetIsPulse(DependencyObject o) => (bool)o.GetValue(IsPulseProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 표시 모서리를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the LED display corner on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>표시 모서리입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The display corner.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetCorner(DependencyObject o, LedCorner v) => o.SetValue(CornerProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 표시 모서리를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the LED display corner from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 모서리입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured corner.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static LedCorner GetCorner(DependencyObject o) => (LedCorner)o.GetValue(CornerProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 지름을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the LED diameter on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>픽셀 단위 지름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The diameter in pixels.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetDiameter(DependencyObject o, double v) => o.SetValue(DiameterProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 지름을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the LED diameter from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>픽셀 단위 지름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The diameter in pixels.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static double GetDiameter(DependencyObject o) => (double)o.GetValue(DiameterProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 가장자리 여백을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the LED edge margin on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>픽셀 단위 여백입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The margin in pixels.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetMargin(DependencyObject o, double v) => o.SetValue(MarginProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 가장자리 여백을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the LED edge margin from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>픽셀 단위 여백입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The margin in pixels.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static double GetMargin(DependencyObject o) => (double)o.GetValue(MarginProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 내부 원 비율을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the LED inner-circle scale on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>내부 원 비율입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The inner-circle scale.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetInnerScale(DependencyObject o, double v) => o.SetValue(InnerScaleProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 LED 내부 원 비율을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the LED inner-circle scale from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>내부 원 비율입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The inner-circle scale.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="o"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is null.</para>
		/// \endif
		/// </exception>
		public static double GetInnerScale(DependencyObject o) => (double)o.GetValue(InnerScaleProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 켜짐 외곽 채우기 브러시를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the on-state outer-fill brush on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The brush to set.</para>
		/// \endif
		/// </param>
		public static void SetOnOuterFill(DependencyObject o, Brush v) => o.SetValue(OnOuterFillProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 켜짐 외곽 채우기 브러시를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the on-state outer-fill brush from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured brush.</para>
		/// \endif
		/// </returns>
		public static Brush GetOnOuterFill(DependencyObject o) => (Brush)o.GetValue(OnOuterFillProperty);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 켜짐 내부 채우기 브러시를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the on-state inner-fill brush on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The brush to set.</para>
		/// \endif
		/// </param>
		public static void SetOnInnerFill(DependencyObject o, Brush v) => o.SetValue(OnInnerFillProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 켜짐 내부 채우기 브러시를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the on-state inner-fill brush from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured brush.</para>
		/// \endif
		/// </returns>
		public static Brush GetOnInnerFill(DependencyObject o) => (Brush)o.GetValue(OnInnerFillProperty);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 꺼짐 외곽 채우기 브러시를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the off-state outer-fill brush on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The brush to set.</para>
		/// \endif
		/// </param>
		public static void SetOffOuterFill(DependencyObject o, Brush v) => o.SetValue(OffOuterFillProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 꺼짐 외곽 채우기 브러시를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the off-state outer-fill brush from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured brush.</para>
		/// \endif
		/// </returns>
		public static Brush GetOffOuterFill(DependencyObject o) => (Brush)o.GetValue(OffOuterFillProperty);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 꺼짐 내부 채우기 브러시를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the off-state inner-fill brush on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The brush to set.</para>
		/// \endif
		/// </param>
		public static void SetOffInnerFill(DependencyObject o, Brush v) => o.SetValue(OffInnerFillProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 꺼짐 내부 채우기 브러시를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the off-state inner-fill brush from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured brush.</para>
		/// \endif
		/// </returns>
		public static Brush GetOffInnerFill(DependencyObject o) => (Brush)o.GetValue(OffInnerFillProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 외곽 링 선 브러시를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the outer-ring stroke brush on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The brush to set.</para>
		/// \endif
		/// </param>
		public static void SetStrokeBrush(DependencyObject o, Brush v) => o.SetValue(StrokeBrushProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 외곽 링 선 브러시를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the outer-ring stroke brush from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured brush.</para>
		/// \endif
		/// </returns>
		public static Brush GetStrokeBrush(DependencyObject o) => (Brush)o.GetValue(StrokeBrushProperty);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 외곽 링 선 두께를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the outer-ring stroke thickness on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>선 두께입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The stroke thickness.</para>
		/// \endif
		/// </param>
		public static void SetStrokeThickness(DependencyObject o, double v) => o.SetValue(StrokeThicknessProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소의 외곽 링 선 두께를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the outer-ring stroke thickness from a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>선 두께입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The stroke thickness.</para>
		/// \endif
		/// </returns>
		public static double GetStrokeThickness(DependencyObject o) => (double)o.GetValue(StrokeThicknessProperty);

		#endregion

		#region Private state
		/// <summary>
		/// \if KO
		/// <para>대상 요소에 설치한 Adorner 컨테이너를 보관하는 내부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal attached property that stores the adorner container installed on a target element.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty AdornerContainerProperty =
			DependencyProperty.RegisterAttached("AdornerContainer", typeof(AdornerContainer), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null));
		/// <summary>
		/// \if KO
		/// <para>대상 요소에 Adorner 컨테이너 참조를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores an adorner-container reference on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>저장할 컨테이너입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The container to store.</para>
		/// \endif
		/// </param>
		private static void SetAdornerContainer(DependencyObject o, AdornerContainer? v) => o.SetValue(AdornerContainerProperty, v);
		/// <summary>
		/// \if KO
		/// <para>대상 요소에 저장된 Adorner 컨테이너를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the adorner container stored on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>저장된 컨테이너이며 없으면 null입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The stored container, or null when absent.</para>
		/// \endif
		/// </returns>
		private static AdornerContainer? GetAdornerContainer(DependencyObject o) => (AdornerContainer?)o.GetValue(AdornerContainerProperty);
		#endregion

		#region Lifecycle
		/// <summary>
		/// \if KO
		/// <para>LED 연결 설정 변경 시 요소 수명 주기 이벤트를 갱신하고 Adorner를 추가하거나 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Updates element lifecycle events and adds or removes the adorner when an attached LED setting changes.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>설정이 변경된 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose setting changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>속성 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The property-change data.</para>
		/// \endif
		/// </param>
		private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not FrameworkElement fe) return;

			if (DesignerProperties.GetIsInDesignMode(fe))
			{
				fe.Loaded -= OnLoaded;
				fe.Loaded += OnLoaded;
				return;
			}

			fe.Loaded -= OnLoaded;
			fe.Unloaded -= OnUnloaded;
			fe.SizeChanged -= OnSizeChanged;

			fe.Loaded += OnLoaded;
			fe.Unloaded += OnUnloaded;
			fe.SizeChanged += OnSizeChanged;

			if (GetIsEnabled(fe)) EnsureAdorner(fe);
			else RemoveAdorner(fe);
		}

		/// <summary>
		/// \if KO
		/// <para>대상 요소가 로드되면 활성화된 LED Adorner를 보장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Ensures an enabled LED adorner when the target element loads.</para>
		/// \endif
		/// </summary>
		/// <param name="s">
		/// \if KO
		/// <para>로드된 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The loaded element.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private static void OnLoaded(object? s, RoutedEventArgs e)
		{
			if (s is FrameworkElement fe && GetIsEnabled(fe)) EnsureAdorner(fe);
		}

		/// <summary>
		/// \if KO
		/// <para>대상 요소가 언로드되면 LED Adorner를 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Removes the LED adorner when the target element unloads.</para>
		/// \endif
		/// </summary>
		/// <param name="s">
		/// \if KO
		/// <para>언로드된 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The unloaded element.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private static void OnUnloaded(object? s, RoutedEventArgs e)
		{
			if (s is FrameworkElement fe) RemoveAdorner(fe);
		}

		/// <summary>
		/// \if KO
		/// <para>대상 요소 크기 변경 알림을 받는 예약 처리기입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Receives target-element size changes as a reserved handler.</para>
		/// \endif
		/// </summary>
		/// <param name="s">
		/// \if KO
		/// <para>크기가 변경된 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The resized element.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전·새 크기 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The old and new size data.</para>
		/// \endif
		/// </param>
		private static void OnSizeChanged(object? s, SizeChangedEventArgs e) { }

		/// <summary>
		/// \if KO
		/// <para>대상 요소의 Adorner 계층에 바인딩된 LED를 만들고 한 번만 추가합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates a bound LED and adds it once to the target element's adorner layer.</para>
		/// \endif
		/// </summary>
		/// <param name="fe">
		/// \if KO
		/// <para>LED를 표시할 대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element over which the LED is displayed.</para>
		/// \endif
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>Adorner 계층이 컨테이너 추가를 허용하지 않으면 WPF에서 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown by WPF when the adorner layer does not permit adding the container.</para>
		/// \endif
		/// </exception>
		private static void EnsureAdorner(FrameworkElement fe)
		{
			var layer = AdornerLayer.GetAdornerLayer(fe);
			if (layer is null) return;

			var container = GetAdornerContainer(fe);
			if (container is null)
			{
				var led = new DreamineCheckLed { IsHitTestVisible = false };

				BindingOperations.SetBinding(led, FrameworkElement.WidthProperty, new Binding("ActualWidth") { Source = fe });
				BindingOperations.SetBinding(led, FrameworkElement.HeightProperty, new Binding("ActualHeight") { Source = fe });

				Bind(led, DreamineCheckLed.IsOnProperty, fe, IsOnProperty);
				Bind(led, DreamineCheckLed.IsPulseProperty, fe, IsPulseProperty);
				Bind(led, DreamineCheckLed.CornerProperty, fe, CornerProperty);
				Bind(led, DreamineCheckLed.DiameterProperty, fe, DiameterProperty);
				Bind(led, DreamineCheckLed.EdgeOffsetProperty, fe, MarginProperty);
				Bind(led, DreamineCheckLed.InnerScaleProperty, fe, InnerScaleProperty);
				Bind(led, DreamineCheckLed.OnOuterFillProperty, fe, OnOuterFillProperty);
				Bind(led, DreamineCheckLed.OnInnerFillProperty, fe, OnInnerFillProperty);
				Bind(led, DreamineCheckLed.OffOuterFillProperty, fe, OffOuterFillProperty);
				Bind(led, DreamineCheckLed.OffInnerFillProperty, fe, OffInnerFillProperty);
				Bind(led, DreamineCheckLed.StrokeBrushProperty, fe, StrokeBrushProperty);
				Bind(led, DreamineCheckLed.StrokeThicknessProperty, fe, StrokeThicknessProperty);

				container = new AdornerContainer(fe) { Child = led, IsHitTestVisible = false };
				layer.Add(container);
				SetAdornerContainer(fe, container);
			}
		}

		/// <summary>
		/// \if KO
		/// <para>대상 요소에 연결된 LED Adorner를 계층에서 제거하고 참조를 비웁니다.</para>
		/// \endif
		/// \if EN
		/// <para>Removes the attached LED adorner from its layer and clears its stored reference.</para>
		/// \endif
		/// </summary>
		/// <param name="fe">
		/// \if KO
		/// <para>LED를 제거할 대상 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target element from which to remove the LED.</para>
		/// \endif
		/// </param>
		private static void RemoveAdorner(FrameworkElement fe)
		{
			var layer = AdornerLayer.GetAdornerLayer(fe);
			var container = GetAdornerContainer(fe);
			if (container != null && layer != null)
			{
				layer.Remove(container);
				SetAdornerContainer(fe, null);
			}
		}

		/// <summary>
		/// \if KO
		/// <para>LED 종속성 속성을 대상 요소의 연결 속성에 바인딩합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Binds an LED dependency property to an attached property on a target element.</para>
		/// \endif
		/// </summary>
		/// <param name="target">
		/// \if KO
		/// <para>바인딩 대상 LED입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target LED.</para>
		/// \endif
		/// </param>
		/// <param name="targetDp">
		/// \if KO
		/// <para>바인딩할 LED 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The LED dependency property to bind.</para>
		/// \endif
		/// </param>
		/// <param name="source">
		/// \if KO
		/// <para>연결 속성을 가진 원본 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The source element that owns the attached property.</para>
		/// \endif
		/// </param>
		/// <param name="attachedDp">
		/// \if KO
		/// <para>바인딩 원본 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The attached source property.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para>필수 개체 또는 속성 인자가 null이면 WPF 바인딩 API에서 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown by the WPF binding API when a required object or property argument is null.</para>
		/// \endif
		/// </exception>
		private static void Bind(DependencyObject target, DependencyProperty targetDp,
								 DependencyObject source, DependencyProperty attachedDp)
		{
			var path = new PropertyPath("(0)", attachedDp);
			BindingOperations.SetBinding(target, targetDp, new Binding { Source = source, Path = path });
		}
		#endregion
	}
}
