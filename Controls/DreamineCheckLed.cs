using Microsoft.Xaml.Behaviors.Layout;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// @brief 버튼 등의 코너에 LED(외곽 원 + 내부 코어)를 오버레이처럼 얹는 경량 컨트롤입니다.
	/// @details 템플릿 내부 Canvas 위에 두 개의 Ellipse(Outer/Inner)를 배치하고,
	/// IsOn/IsPulse에 따라 브러시와 애니메이션을 전환합니다.
	/// </summary>
	public class DreamineCheckLed : Control
	{
		private static readonly Brush _onOuter = Freeze(CreateRadial(
			new[] { ("#FF1FD36B", 0.00), ("#FFA7F0C1", 0.60), ("#00FFFFFF", 1.00) }, 0.6, 0.6));
		private static readonly Brush _onInner = Freeze(CreateRadial(
			new[] { ("#FF6E6E6E", 0.00), ("#FF4B4B4B", 1.00) }, 0.65, 0.65));
		// Off 상태는 "링"만 보여야 하므로 Fill은 투명으로 둡니다.
		private static readonly Brush _offOuter = Brushes.Transparent;
		private static readonly Brush _offInner = Brushes.Transparent;

		/// <summary> @brief 방사형 그라데이션 브러시 생성 유틸 </summary>
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
		private static Brush Freeze(Brush b) { if (b.CanFreeze) b.Freeze(); return b; }

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


		/// <summary> @brief LED On/Off 상태 </summary>
		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(DreamineCheckLed),
				new PropertyMetadata(false));

		/// <summary> @brief LED가 숨쉬기(pulse) 애니메이션을 수행할지 여부 </summary>
		public static readonly DependencyProperty IsPulseProperty =
			DependencyProperty.Register(nameof(IsPulse), typeof(bool), typeof(DreamineCheckLed),
				new PropertyMetadata(false));

		/// <summary> @brief 표시 코너 </summary>
		public static readonly DependencyProperty CornerProperty =
			DependencyProperty.Register(nameof(Corner), typeof(LedCorner), typeof(DreamineCheckLed),
				new PropertyMetadata(LedCorner.TopRight));

		/// <summary> @brief LED 지름(px) </summary>
		public static readonly DependencyProperty DiameterProperty =
			DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(18.0));

		/// <summary> @brief 가장자리 여백(px) </summary>
		public static readonly DependencyProperty EdgeOffsetProperty =
			DependencyProperty.Register(nameof(EdgeOffset), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(6.0));

		/// <summary> @brief 내부 원 지름 비율(0~1). 기본 0.45 </summary>
		public static readonly DependencyProperty InnerScaleProperty =
			DependencyProperty.Register(nameof(InnerScale), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(0.45));

		/// <summary> @brief On/Off 브러시들 </summary>
		public static readonly DependencyProperty OnOuterFillProperty =
			DependencyProperty.Register(nameof(OnOuterFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_onOuter));
		public static readonly DependencyProperty OnInnerFillProperty =
			DependencyProperty.Register(nameof(OnInnerFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_onInner));
		public static readonly DependencyProperty OffOuterFillProperty =
			DependencyProperty.Register(nameof(OffOuterFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_offOuter));
		public static readonly DependencyProperty OffInnerFillProperty =
			DependencyProperty.Register(nameof(OffInnerFill), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(_offInner));

		/// <summary> @brief 외곽선(링) 브러시/두께 </summary>
		public static readonly DependencyProperty StrokeBrushProperty =
			DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(DreamineCheckLed),
				new PropertyMetadata(Brushes.White));
		public static readonly DependencyProperty StrokeThicknessProperty =
			DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(DreamineCheckLed),
				new PropertyMetadata(1.2));

		/// <summary>LED On/Off</summary>
		public bool IsOn { get => (bool)GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }
		/// <summary>Pulse 애니메이션 사용</summary>
		public bool IsPulse { get => (bool)GetValue(IsPulseProperty); set => SetValue(IsPulseProperty, value); }
		/// <summary>표시 코너</summary>
		public LedCorner Corner { get => (LedCorner)GetValue(CornerProperty); set => SetValue(CornerProperty, value); }
		/// <summary>LED 지름(px)</summary>
		public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
		/// <summary>가장자리 여백(px)</summary>
		public double EdgeOffset { get => (double)GetValue(EdgeOffsetProperty); set => SetValue(EdgeOffsetProperty, value); }
		/// <summary>내부 원 지름 비율(0~1)</summary>
		public double InnerScale { get => (double)GetValue(InnerScaleProperty); set => SetValue(InnerScaleProperty, value); }
		/// <summary>On 외곽 브러시</summary>
		public Brush OnOuterFill { get => (Brush)GetValue(OnOuterFillProperty); set => SetValue(OnOuterFillProperty, value); }
		/// <summary>On 내부 브러시</summary>
		public Brush OnInnerFill { get => (Brush)GetValue(OnInnerFillProperty); set => SetValue(OnInnerFillProperty, value); }
		/// <summary>Off 외곽 브러시(기본 투명)</summary>
		public Brush OffOuterFill { get => (Brush)GetValue(OffOuterFillProperty); set => SetValue(OffOuterFillProperty, value); }
		/// <summary>Off 내부 브러시(기본 투명)</summary>
		public Brush OffInnerFill { get => (Brush)GetValue(OffInnerFillProperty); set => SetValue(OffInnerFillProperty, value); }
		/// <summary>외곽선 브러시</summary>
		public Brush StrokeBrush { get => (Brush)GetValue(StrokeBrushProperty); set => SetValue(StrokeBrushProperty, value); }
		/// <summary>외곽선 두께</summary>
		public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
	}



	/// <summary>
	/// @brief 임의 요소 위에 <see cref="DreamineCheckLed"/>를 Adorner로 얹어주는 Attached 래퍼
	/// @details 붙여 쓰기 예:
	/// @code{.xml}
	/// <VsControls:DreamineButton
	///   VsControls:DreamineCheckLedAttach.IsEnabled="True"
	///   VsControls:DreamineCheckLedAttach.IsOn="{Binding PusherExtendLed}"
	///   VsControls:DreamineCheckLedAttach.IsPulse="True"
	///   VsControls:DreamineCheckLedAttach.Corner="TopRight"
	///   VsControls:DreamineCheckLedAttach.Diameter="20"
	///   VsControls:DreamineCheckLedAttach.Margin="6"/>
	/// @endcode
	/// </summary>
	public static class DreamineCheckLedAttach
	{
		#region Public Attached DPs

		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(false, OnAnyPropertyChanged));

		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.RegisterAttached("IsOn", typeof(bool), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(false, OnAnyPropertyChanged));

		/// <summary>@brief Pulse 애니메이션 사용 여부</summary>
		public static readonly DependencyProperty IsPulseProperty =
			DependencyProperty.RegisterAttached("IsPulse", typeof(bool), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(false, OnAnyPropertyChanged));

		public static readonly DependencyProperty CornerProperty =
			DependencyProperty.RegisterAttached("Corner", typeof(LedCorner), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(LedCorner.TopRight, OnAnyPropertyChanged));

		public static readonly DependencyProperty DiameterProperty =
			DependencyProperty.RegisterAttached("Diameter", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(18.0, OnAnyPropertyChanged));

		/// <summary>@brief 가장자리 여백(px) — 이름은 Margin(double)로 노출</summary>
		public static readonly DependencyProperty MarginProperty =
			DependencyProperty.RegisterAttached("Margin", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(6.0, OnAnyPropertyChanged));

		public static readonly DependencyProperty InnerScaleProperty =
			DependencyProperty.RegisterAttached("InnerScale", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(0.45, OnAnyPropertyChanged));

		public static readonly DependencyProperty OnOuterFillProperty =
			DependencyProperty.RegisterAttached("OnOuterFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));
		public static readonly DependencyProperty OnInnerFillProperty =
			DependencyProperty.RegisterAttached("OnInnerFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));
		public static readonly DependencyProperty OffOuterFillProperty =
			DependencyProperty.RegisterAttached("OffOuterFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));
		public static readonly DependencyProperty OffInnerFillProperty =
			DependencyProperty.RegisterAttached("OffInnerFill", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null, OnAnyPropertyChanged));

		public static readonly DependencyProperty StrokeBrushProperty =
			DependencyProperty.RegisterAttached("StrokeBrush", typeof(Brush), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(Brushes.White, OnAnyPropertyChanged));
		public static readonly DependencyProperty StrokeThicknessProperty =
			DependencyProperty.RegisterAttached("StrokeThickness", typeof(double), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(1.2, OnAnyPropertyChanged));

		// Get/Set
		public static void SetIsEnabled(DependencyObject o, bool v) => o.SetValue(IsEnabledProperty, v);
		public static bool GetIsEnabled(DependencyObject o) => (bool)o.GetValue(IsEnabledProperty);

		public static void SetIsOn(DependencyObject o, bool v) => o.SetValue(IsOnProperty, v);
		public static bool GetIsOn(DependencyObject o) => (bool)o.GetValue(IsOnProperty);

		public static void SetIsPulse(DependencyObject o, bool v) => o.SetValue(IsPulseProperty, v);
		public static bool GetIsPulse(DependencyObject o) => (bool)o.GetValue(IsPulseProperty);

		public static void SetCorner(DependencyObject o, LedCorner v) => o.SetValue(CornerProperty, v);
		public static LedCorner GetCorner(DependencyObject o) => (LedCorner)o.GetValue(CornerProperty);

		public static void SetDiameter(DependencyObject o, double v) => o.SetValue(DiameterProperty, v);
		public static double GetDiameter(DependencyObject o) => (double)o.GetValue(DiameterProperty);

		public static void SetMargin(DependencyObject o, double v) => o.SetValue(MarginProperty, v);
		public static double GetMargin(DependencyObject o) => (double)o.GetValue(MarginProperty);

		public static void SetInnerScale(DependencyObject o, double v) => o.SetValue(InnerScaleProperty, v);
		public static double GetInnerScale(DependencyObject o) => (double)o.GetValue(InnerScaleProperty);

		public static void SetOnOuterFill(DependencyObject o, Brush v) => o.SetValue(OnOuterFillProperty, v);
		public static Brush GetOnOuterFill(DependencyObject o) => (Brush)o.GetValue(OnOuterFillProperty);
		public static void SetOnInnerFill(DependencyObject o, Brush v) => o.SetValue(OnInnerFillProperty, v);
		public static Brush GetOnInnerFill(DependencyObject o) => (Brush)o.GetValue(OnInnerFillProperty);
		public static void SetOffOuterFill(DependencyObject o, Brush v) => o.SetValue(OffOuterFillProperty, v);
		public static Brush GetOffOuterFill(DependencyObject o) => (Brush)o.GetValue(OffOuterFillProperty);
		public static void SetOffInnerFill(DependencyObject o, Brush v) => o.SetValue(OffInnerFillProperty, v);
		public static Brush GetOffInnerFill(DependencyObject o) => (Brush)o.GetValue(OffInnerFillProperty);

		public static void SetStrokeBrush(DependencyObject o, Brush v) => o.SetValue(StrokeBrushProperty, v);
		public static Brush GetStrokeBrush(DependencyObject o) => (Brush)o.GetValue(StrokeBrushProperty);
		public static void SetStrokeThickness(DependencyObject o, double v) => o.SetValue(StrokeThicknessProperty, v);
		public static double GetStrokeThickness(DependencyObject o) => (double)o.GetValue(StrokeThicknessProperty);

		#endregion

		#region Private state
		private static readonly DependencyProperty AdornerContainerProperty =
			DependencyProperty.RegisterAttached("AdornerContainer", typeof(AdornerContainer), typeof(DreamineCheckLedAttach),
				new PropertyMetadata(null));
		private static void SetAdornerContainer(DependencyObject o, AdornerContainer? v) => o.SetValue(AdornerContainerProperty, v);
		private static AdornerContainer? GetAdornerContainer(DependencyObject o) => (AdornerContainer?)o.GetValue(AdornerContainerProperty);
		#endregion

		#region Lifecycle
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

		private static void OnLoaded(object? s, RoutedEventArgs e)
		{
			if (s is FrameworkElement fe && GetIsEnabled(fe)) EnsureAdorner(fe);
		}

		private static void OnUnloaded(object? s, RoutedEventArgs e)
		{
			if (s is FrameworkElement fe) RemoveAdorner(fe);
		}

		private static void OnSizeChanged(object? s, SizeChangedEventArgs e) { }

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

		/// <summary>@brief DreamineCheckLed의 DP를 대상 요소의 Attached DP에 바인딩합니다.</summary>
		private static void Bind(DependencyObject target, DependencyProperty targetDp,
								 DependencyObject source, DependencyProperty attachedDp)
		{
			var path = new PropertyPath("(0)", attachedDp);
			BindingOperations.SetBinding(target, targetDp, new Binding { Source = source, Path = path });
		}
		#endregion
	}
}