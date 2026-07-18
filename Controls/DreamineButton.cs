// \file DreamineButton.cs
// \brief Custom button control for VsLibrary framework. Supports Icon/Shadow/Permission/AttachedCommand.

using System;
using System.ComponentModel; // \brief DesignerProperties
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Dreamine.UI.Wpf.Controls.MessageBox;
using Dreamine.UI.Wpf.Controls.Navigation;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>선택 상태를 표시할 시각적 방식을 정의합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Defines how selection state is presented visually.</para>
	/// \endif
	/// </summary>
	public enum SelectedVisualMode
	{
		/// <summary>
		/// \if KO
		/// <para>테두리만 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays only the border.</para>
		/// \endif
		/// </summary>
		BorderOnly = 0,
		/// <summary>
		/// \if KO
		/// <para>배경만 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays only the background.</para>
		/// \endif
		/// </summary>
		BackgroundOnly = 1,
		/// <summary>
		/// \if KO
		/// <para>테두리와 배경을 모두 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays both border and background.</para>
		/// \endif
		/// </summary>
		Both = 2
	}

	/// <summary>
	/// \if KO
	/// <para>버튼 콘텐츠에 대한 아이콘 배치를 정의합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Defines icon placement relative to button content.</para>
	/// \endif
	/// </summary>
	public enum IconPosition
	{
		/// <summary>
		/// \if KO
		/// <para>왼쪽입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the icon on the left.</para>
		/// \endif
		/// </summary>
		Left,
		/// <summary>
		/// \if KO
		/// <para>오른쪽입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the icon on the right.</para>
		/// \endif
		/// </summary>
		Right,
		/// <summary>
		/// \if KO
		/// <para>위쪽입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the icon at the top.</para>
		/// \endif
		/// </summary>
		Top,
		/// <summary>
		/// \if KO
		/// <para>아래쪽입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the icon at the bottom.</para>
		/// \endif
		/// </summary>
		Bottom,
		/// <summary>
		/// \if KO
		/// <para>전체 영역을 채웁니다.</para>
		/// \endif
		/// \if EN
		/// <para>Fills the available area.</para>
		/// \endif
		/// </summary>
		Full
	}

	/**
	 * \class DreamineButton
	 * \brief Custom button control for VsLibrary framework.
	 */
	/// <summary>
	/// \if KO
	/// <para>아이콘, 그림자, 선택 표시, 권한 검사 및 이벤트 기반 연결 명령을 제공하는 버튼입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Represents a button with icons, shadows, selection visuals, permission checks, and event-based attached commands.</para>
	/// \endif
	/// </summary>
	public class DreamineButton : Button
	{

		#region Template parts

		/** \brief Template part name for the shadow host. */
		/// <summary>
		/// \if KO
		/// <para>그림자 효과를 적용할 템플릿 파트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the template-part name that receives the shadow effect.</para>
		/// \endif
		/// </summary>
		private const string PART_ShadowHost = "ShadowHost";

		/** \brief Shadow host of the current template. */
		/// <summary>
		/// \if KO
		/// <para>현재 템플릿에서 확인한 그림자 호스트를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the shadow host resolved from the current template.</para>
		/// \endif
		/// </summary>
		private Border? _shadowHost;

		#endregion

		#region New DPs : IconSize / IconMargin / IconStretch

		/**
		* \brief Controls the default icon size (Width/Height) at once.
		*/
		/// <summary>
		/// \if KO
		/// <para>아이콘의 기본 너비와 높이를 함께 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the default icon width and height together.</para>
		/// \endif
		/// </summary>
		public double IconSize
		{
			get => (double)GetValue(IconSizeProperty);
			set => SetValue(IconSizeProperty, value);
		}

		/** \brief DP for \ref IconSize. */
		/// <summary>
		/// \if KO
		/// <para><see cref="IconSize"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="IconSize"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IconSizeProperty =
			DependencyProperty.Register(
				nameof(IconSize),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					20d,
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange |
					FrameworkPropertyMetadataOptions.AffectsRender));

		/**
		 * \brief Controls the icon margin.
		 */
		/// <summary>
		/// \if KO
		/// <para>아이콘 여백을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the icon margin.</para>
		/// \endif
		/// </summary>
		public Thickness IconMargin
		{
			get => (Thickness)GetValue(IconMarginProperty);
			set => SetValue(IconMarginProperty, value);
		}

		/** \brief DP for \ref IconMargin. */
		/// <summary>
		/// \if KO
		/// <para><see cref="IconMargin"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="IconMargin"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IconMarginProperty =
			DependencyProperty.Register(
				nameof(IconMargin),
				typeof(Thickness),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					new Thickness(0, 0, 4, 0),
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange));

		/**
		 * \brief Icon stretch mode.
		 */
		/// <summary>
		/// \if KO
		/// <para>아이콘 늘이기 방식을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the icon stretch mode.</para>
		/// \endif
		/// </summary>
		public Stretch IconStretch
		{
			get => (Stretch)GetValue(IconStretchProperty);
			set => SetValue(IconStretchProperty, value);
		}

		/** \brief DP for \ref IconStretch. */
		/// <summary>
		/// \if KO
		/// <para><see cref="IconStretch"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="IconStretch"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IconStretchProperty =
			DependencyProperty.Register(
				nameof(IconStretch),
				typeof(Stretch),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					Stretch.Uniform,
					FrameworkPropertyMetadataOptions.AffectsRender));

		#endregion

		#region New DPs : Shadow (Effect)

		/**
		 * \brief Enables or disables DropShadowEffect.
		 */
		/// <summary>
		/// \if KO
		/// <para>그림자 효과 사용 여부를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the drop-shadow effect is enabled.</para>
		/// \endif
		/// </summary>
		public bool UseShadow
		{
			get => (bool)GetValue(UseShadowProperty);
			set => SetValue(UseShadowProperty, value);
		}

		/** \brief DP for \ref UseShadow. */
		/// <summary>
		/// \if KO
		/// <para><see cref="UseShadow"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="UseShadow"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty UseShadowProperty =
			DependencyProperty.Register(
				nameof(UseShadow),
				typeof(bool),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					false,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow blur radius. */
		/// <summary>
		/// \if KO
		/// <para>그림자 흐림 반경을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the shadow blur radius.</para>
		/// \endif
		/// </summary>
		public double ShadowBlurRadius
		{
			get => (double)GetValue(ShadowBlurRadiusProperty);
			set => SetValue(ShadowBlurRadiusProperty, value);
		}

		/** \brief DP for \ref ShadowBlurRadius. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ShadowBlurRadius"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ShadowBlurRadius"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShadowBlurRadiusProperty =
			DependencyProperty.Register(
				nameof(ShadowBlurRadius),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					18d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow opacity. */
		/// <summary>
		/// \if KO
		/// <para>그림자 불투명도를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the shadow opacity.</para>
		/// \endif
		/// </summary>
		public double ShadowOpacity
		{
			get => (double)GetValue(ShadowOpacityProperty);
			set => SetValue(ShadowOpacityProperty, value);
		}

		/** \brief DP for \ref ShadowOpacity. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ShadowOpacity"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ShadowOpacity"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShadowOpacityProperty =
			DependencyProperty.Register(
				nameof(ShadowOpacity),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					0.55d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow depth. */
		/// <summary>
		/// \if KO
		/// <para>그림자 깊이를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the shadow depth.</para>
		/// \endif
		/// </summary>
		public double ShadowDepth
		{
			get => (double)GetValue(ShadowDepthProperty);
			set => SetValue(ShadowDepthProperty, value);
		}

		/** \brief DP for \ref ShadowDepth. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ShadowDepth"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ShadowDepth"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShadowDepthProperty =
			DependencyProperty.Register(
				nameof(ShadowDepth),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					6d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/**
		 * \brief Shadow direction in degrees. 0=Left, 90=Down, 180=Right, 270=Up.
		 */
		/// <summary>
		/// \if KO
		/// <para>그림자 방향을 각도로 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the shadow direction in degrees.</para>
		/// \endif
		/// </summary>
		public double ShadowDirection
		{
			get => (double)GetValue(ShadowDirectionProperty);
			set => SetValue(ShadowDirectionProperty, value);
		}

		/** \brief DP for \ref ShadowDirection. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ShadowDirection"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ShadowDirection"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShadowDirectionProperty =
			DependencyProperty.Register(
				nameof(ShadowDirection),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					90d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow color. */
		/// <summary>
		/// \if KO
		/// <para>그림자 색상을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the shadow color.</para>
		/// \endif
		/// </summary>
		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}

		/** \brief DP for \ref ShadowColor. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ShadowColor"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ShadowColor"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShadowColorProperty =
			DependencyProperty.Register(
				nameof(ShadowColor),
				typeof(Color),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					Colors.Black,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/**
		 * \brief Updates template Effect when any shadow DP changes.
		 */
		/// <summary>
		/// \if KO
		/// <para>그림자 관련 값이 변경되면 해당 버튼의 템플릿 효과를 갱신합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Updates the button's template effect when a shadow-related value changes.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>값이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose value changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 값과 새 값을 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the old and new values.</para>
		/// \endif
		/// </param>
		private static void OnShadowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineButton btn)
				btn.UpdateShadowEffect();
		}

		#endregion

		#region Existing : ImagePosition / OnceLogin / Login / IconSource / IconPath / Command etc.

		/**
		 * \brief Dependency property for selection visual presentation mode.
		 */
		/// <summary>
		/// \if KO
		/// <para>선택 상태의 시각적 표시 방식을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the visual presentation mode for selection.</para>
		/// \endif
		/// </summary>
		public SelectedVisualMode SelectedVisualMode
		{
			get => (SelectedVisualMode)GetValue(SelectedVisualModeProperty);
			set => SetValue(SelectedVisualModeProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedVisualMode.
		 */
		/// <summary>
		/// \if KO
		/// <para><see cref="SelectedVisualMode"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="SelectedVisualMode"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty SelectedVisualModeProperty =
			DependencyProperty.Register(
				nameof(SelectedVisualMode),
				typeof(SelectedVisualMode),
				typeof(DreamineButton),
				new PropertyMetadata(SelectedVisualMode.BorderOnly));

		/**
		 * \brief Selected border brush.
		 */
		/// <summary>
		/// \if KO
		/// <para>선택 테두리 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the selected-border brush.</para>
		/// \endif
		/// </summary>
		public Brush SelectedBorderBrush
		{
			get => (Brush)GetValue(SelectedBorderBrushProperty);
			set => SetValue(SelectedBorderBrushProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedBorderBrush.
		 */
		/// <summary>
		/// \if KO
		/// <para><see cref="SelectedBorderBrush"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="SelectedBorderBrush"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty SelectedBorderBrushProperty =
			DependencyProperty.Register(
				nameof(SelectedBorderBrush),
				typeof(Brush),
				typeof(DreamineButton),
				new PropertyMetadata(Brushes.Blue));

		/**
		 * \brief Selected border thickness.
		 */
		/// <summary>
		/// \if KO
		/// <para>선택 테두리 두께를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the selected-border thickness.</para>
		/// \endif
		/// </summary>
		public Thickness SelectedBorderThickness
		{
			get => (Thickness)GetValue(SelectedBorderThicknessProperty);
			set => SetValue(SelectedBorderThicknessProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedBorderThickness.
		 */
		/// <summary>
		/// \if KO
		/// <para><see cref="SelectedBorderThickness"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="SelectedBorderThickness"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty SelectedBorderThicknessProperty =
			DependencyProperty.Register(
				nameof(SelectedBorderThickness),
				typeof(Thickness),
				typeof(DreamineButton),
				new PropertyMetadata(new Thickness(3)));

		/**
		 * \brief Selected background overlay brush (alpha recommended).
		 * \details Template overlay uses this brush for BackgroundOnly/Both modes.
		 */
		/// <summary>
		/// \if KO
		/// <para>선택 배경 오버레이 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the selected-background overlay brush.</para>
		/// \endif
		/// </summary>
		public Brush SelectedBackgroundOverlay
		{
			get => (Brush)GetValue(SelectedBackgroundOverlayProperty);
			set => SetValue(SelectedBackgroundOverlayProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedBackgroundOverlay.
		 */
		/// <summary>
		/// \if KO
		/// <para><see cref="SelectedBackgroundOverlay"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="SelectedBackgroundOverlay"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty SelectedBackgroundOverlayProperty =
			DependencyProperty.Register(
				nameof(SelectedBackgroundOverlay),
				typeof(Brush),
				typeof(DreamineButton),
				new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x30, 0x00, 0x7A, 0xFF))));

		/** \brief Icon layout position. */
		/// <summary>
		/// \if KO
		/// <para>아이콘 배치 위치를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the icon layout position.</para>
		/// \endif
		/// </summary>
		public IconPosition ImagePosition
		{
			get => (IconPosition)GetValue(ImagePositionProperty);
			set => SetValue(ImagePositionProperty, value);
		}

		/** \brief DP for \ref ImagePosition. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ImagePosition"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ImagePosition"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ImagePositionProperty =
			DependencyProperty.Register(
				nameof(ImagePosition),
				typeof(IconPosition),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					IconPosition.Left,
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange));

		/** \brief Enables one-time login session reuse. */
		/// <summary>
		/// \if KO
		/// <para>한 번 로그인 세션 재사용 여부를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether a one-time login session is reused.</para>
		/// \endif
		/// </summary>
		public bool OnceLogin
		{
			get => (bool)GetValue(OnceLoginProperty);
			set => SetValue(OnceLoginProperty, value);
		}

		/** \brief DP for \ref OnceLogin. */
		/// <summary>
		/// \if KO
		/// <para><see cref="OnceLogin"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="OnceLogin"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OnceLoginProperty =
			DependencyProperty.Register(
				nameof(OnceLogin),
				typeof(bool),
				typeof(DreamineButton),
				new PropertyMetadata(false, OnOnceLoginChanged));

		/**
		 * \brief Updates global navigation helper when OnceLogin changes.
		 */
		/// <summary>
		/// \if KO
		/// <para>한 번 로그인 설정 변경을 전역 탐색 도우미에 반영합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Propagates a once-login setting change to the global navigation helper.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>값이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose value changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>새 값을 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the new value.</para>
		/// \endif
		/// </param>
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>새 값이 <see cref="bool"/>이 아닐 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the new value is not a <see cref="bool"/>.</para>
		/// \endif
		/// </exception>
		private static void OnOnceLoginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineButton)
				VsNavigationHelper.IsOnceLoginEnabled = (bool)e.NewValue;
		}

		/**
		 * \brief Applies default style key and auto-merges ResourceDictionary.
		 */
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 등록하고 버튼 테마 리소스를 애플리케이션에 한 번 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Registers the default style key and merges the button theme resource into the application once.</para>
		/// \endif
		/// </summary>
		static DreamineButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(typeof(DreamineButton)));

			try
			{
				var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineButtonStyle.xaml",
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
				// \brief Guard for design-time / missing resources, etc.
			}

		}

		/**
		 * \brief Constructor: subscribes to login changed event.
		 */
		/// <summary>
		/// \if KO
		/// <para>새 버튼 인스턴스를 초기화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a new button instance.</para>
		/// \endif
		/// </summary>
		public DreamineButton()
		{
		}

		/**
		 * \brief Caches template parts and updates shadow effect after template is applied.
		 */
		/// <summary>
		/// \if KO
		/// <para>템플릿 적용 후 그림자 호스트를 확인하고 효과를 갱신합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Resolves the shadow host and updates its effect after the template is applied.</para>
		/// \endif
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_shadowHost = GetTemplateChild(PART_ShadowHost) as Border;

			// \brief If template part name mismatches, _shadowHost can be null.
			UpdateShadowEffect();
		}

		/**
		 * \brief Applies shadow effect to ShadowHost.
		 */
		/// <summary>
		/// \if KO
		/// <para>현재 그림자 설정을 템플릿 호스트에 적용하거나 효과를 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies current shadow settings to the template host or removes its effect.</para>
		/// \endif
		/// </summary>
		private void UpdateShadowEffect()
		{
			if (_shadowHost == null)
				return;

			if (!UseShadow)
			{
				_shadowHost.Effect = null;
				return;
			}

			// \brief Avoid background/brush interference: use Effect only on transparent host.
			_shadowHost.Effect = new DropShadowEffect
			{
				BlurRadius = ShadowBlurRadius,
				Opacity = ShadowOpacity,
				ShadowDepth = ShadowDepth,
				Direction = ShadowDirection,
				Color = ShadowColor
			};
		}

		/** \brief Bitmap icon source. */
		/// <summary>
		/// \if KO
		/// <para>비트맵 아이콘 원본을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the bitmap icon source.</para>
		/// \endif
		/// </summary>
		public ImageSource ImageSource
		{
			get => (ImageSource)GetValue(ImageSourceProperty);
			set => SetValue(ImageSourceProperty, value);
		}

		/** \brief DP for \ref ImageSource. */
		/// <summary>
		/// \if KO
		/// <para><see cref="ImageSource"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ImageSource"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ImageSourceProperty =
			DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(DreamineButton),
				new PropertyMetadata(null));

		/** \brief Vector icon geometry (Path). */
		/// <summary>
		/// \if KO
		/// <para>벡터 아이콘 기하 도형을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the vector icon geometry.</para>
		/// \endif
		/// </summary>
		public Geometry IconPath
		{
			get => (Geometry)GetValue(IconPathProperty);
			set => SetValue(IconPathProperty, value);
		}

		/** \brief DP for \ref IconPath. */
		/// <summary>
		/// \if KO
		/// <para><see cref="IconPath"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="IconPath"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IconPathProperty =
			DependencyProperty.Register(nameof(IconPath), typeof(Geometry), typeof(DreamineButton),
				new PropertyMetadata(null));

		/** \brief Attached command (separate from Button.Command). */
		/// <summary>
		/// \if KO
		/// <para>이벤트 트리거에서 실행할 버튼 명령 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the button command dependency property executed by event triggers.</para>
		/// \endif
		/// </summary>
		public new static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register(
				nameof(Command),
				typeof(ICommand),
				typeof(DreamineButton),
				new PropertyMetadata(null, OnCommandChanged));

		/** \brief Command get/set. */
		/// <summary>
		/// \if KO
		/// <para>이벤트 트리거에서 실행할 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the command executed by event triggers.</para>
		/// \endif
		/// </summary>
		public new ICommand Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		/** \brief Attached command parameter. */
		/// <summary>
		/// \if KO
		/// <para>연결 명령 매개변수 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached command-parameter property.</para>
		/// \endif
		/// </summary>
		public new static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineButton),
				new PropertyMetadata(null));

		/** \brief Sets CommandParameter attached property. */
		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 명령 매개변수를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command parameter on the specified object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the value.</para>
		/// \endif
		/// </param>
		/// <param name="value">
		/// \if KO
		/// <para>명령에 전달할 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The value passed to the command.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/** \brief Gets CommandParameter attached property. */
		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 명령 매개변수를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command parameter from the specified object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 명령 매개변수입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured command parameter.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/** \brief Comma-separated trigger event names. */
		/// <summary>
		/// \if KO
		/// <para>쉼표 구분 명령 트리거 이벤트 이름 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the comma-separated command-trigger event-name property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineButton),
				new PropertyMetadata("PreviewMouseUp"));

		/** \brief Sets CommandTriggerName attached property. */
		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 명령 트리거 이름 목록을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command-trigger name list on the specified object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the value.</para>
		/// \endif
		/// </param>
		/// <param name="value">
		/// \if KO
		/// <para>쉼표 구분 이벤트 이름 목록입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The comma-separated event-name list.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/** \brief Gets CommandTriggerName attached property. */
		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 명령 트리거 이름 목록을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command-trigger name list from the specified object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>쉼표 구분 이벤트 이름 목록입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The comma-separated event-name list.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public static string GetCommandTriggerName(DependencyObject obj)
			=> (string)obj.GetValue(CommandTriggerNameProperty);

		/** \brief Attached Command helper set. */
		/// <summary>
		/// \if KO
		/// <para>지정한 객체에 연결 명령을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the attached command on the specified object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>명령을 설정할 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the command.</para>
		/// \endif
		/// </param>
		/// <param name="value">
		/// \if KO
		/// <para>실행할 명령입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The command to execute.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

		/** \brief Attached Command helper get. */
		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 연결 명령을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the attached command from the specified object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>명령을 읽을 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object from which to read the command.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>연결된 명령입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The attached command.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/** \brief Prevents duplicate event handler hooking. */
		/// <summary>
		/// \if KO
		/// <para>이벤트 처리기 중복 연결 방지 플래그를 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the flag that prevents duplicate event-handler hookup.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached("IsHandlersHooked", typeof(bool), typeof(DreamineButton),
				new PropertyMetadata(false));

		/** \brief Gets IsHandlersHooked attached property. */
		/// <summary>
		/// \if KO
		/// <para>이벤트 처리기 연결 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether event handlers are connected.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>상태 소유 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The state owner.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>연결 여부입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The connection state.</para>
		/// \endif
		/// </returns>
		private static bool GetIsHandlersHooked(DependencyObject d) => (bool)d.GetValue(IsHandlersHookedProperty);

		/** \brief Sets IsHandlersHooked attached property. */
		/// <summary>
		/// \if KO
		/// <para>이벤트 처리기 연결 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether event handlers are connected.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>상태 소유 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The state owner.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The state to set.</para>
		/// \endif
		/// </param>
		private static void SetIsHandlersHooked(DependencyObject d, bool v) => d.SetValue(IsHandlersHookedProperty, v);

		/** \brief Reentrancy guard flag. */
		/// <summary>
		/// \if KO
		/// <para>명령 재진입 방지 플래그를 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the command reentrancy-guard flag.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsExecutingProperty =
			DependencyProperty.RegisterAttached("IsExecuting", typeof(bool), typeof(DreamineButton),
				new PropertyMetadata(false));

		/** \brief Gets IsExecuting attached property. */
		/// <summary>
		/// \if KO
		/// <para>명령 실행 중 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether a command is executing.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>상태 소유 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The state owner.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>실행 중 여부입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The execution state.</para>
		/// \endif
		/// </returns>
		private static bool GetIsExecuting(DependencyObject d) => (bool)d.GetValue(IsExecutingProperty);

		/** \brief Sets IsExecuting attached property. */
		/// <summary>
		/// \if KO
		/// <para>명령 실행 중 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether a command is executing.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>상태 소유 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The state owner.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>설정할 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The state to set.</para>
		/// \endif
		/// </param>
		private static void SetIsExecuting(DependencyObject d, bool v) => d.SetValue(IsExecutingProperty, v);

		/**
		 * \brief Hooks internal event handlers once when Command changes.
		 */
		/// <summary>
		/// \if KO
		/// <para>명령이 설정되면 지원되는 입력 및 클릭 이벤트 처리기를 한 번 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Hooks supported input and click event handlers once when a command is assigned.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>명령이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose command changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 명령과 새 명령을 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the old and new commands.</para>
		/// \endif
		/// </param>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not UIElement element) return;
			if (GetIsHandlersHooked(d)) return;

			element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "PreviewMouseUp", ev);
			}), true);

			element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "MouseDoubleClick", ev);
			}), true);

			element.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "PreviewKeyUp", ev);
			}), true);

			element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, ev) =>
			{
				TryExecuteCommand(d, "TouchUp", ev);
			}));

			element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "Click", ev);
			}));

			SetIsHandlersHooked(d, true);
		}

		/**
 * \brief Executes the command only when the trigger name matches.
 */
		/// <summary>
		/// \if KO
		/// <para>트리거 필터, 재진입 방지 및 버튼 권한 검사를 거쳐 연결 명령을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the attached command after trigger filtering, reentrancy guarding, and button permission checks.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>이벤트를 발생시킨 대상 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target object that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="eventName">
		/// \if KO
		/// <para>라우트 이벤트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event name.</para>
		/// \endif
		/// </param>
		/// <param name="eventArgs">
		/// \if KO
		/// <para>원래 라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The original routed-event data.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="d"/> 또는 <paramref name="eventName"/>이 <see langword="null"/>일 때 비동기 처리 중 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown asynchronously when <paramref name="d"/> or <paramref name="eventName"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>명령 실행 후 <paramref name="eventArgs"/>가 <see langword="null"/>이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown after command execution when <paramref name="eventArgs"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		/// <remarks>
		/// \if KO
		/// <para><see langword="async void"/> 이벤트 처리기이므로 호출자는 실행 예외를 직접 대기할 수 없습니다.</para>
		/// \endif
		/// \if EN
		/// <para>This is an <see langword="async void"/> event handler, so callers cannot directly await execution exceptions.</para>
		/// \endif
		/// </remarks>
		private static async void TryExecuteCommand(DependencyObject d, string eventName, RoutedEventArgs eventArgs)
		{
			await Task.Yield();

			var rawTrigger = GetCommandTriggerName(d);
			if (string.IsNullOrWhiteSpace(rawTrigger))
				return;

			var triggers = rawTrigger
				.Split(',')
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.ToArray();

			if (triggers.Length > 0 &&
				!triggers.Contains(eventName, StringComparer.OrdinalIgnoreCase))
				return;

			var command = GetCommand(d);

			// Resolve command parameter with a clear priority:
			// 1) DreamineButton attached CommandParameter.
			// 2) WPF ButtonBase.CommandParameter.
			// 3) RoutedEventArgs fallback.
			object? parameter = null;

			// 1) Attached CommandParameter (DreamineButton).
			parameter = GetCommandParameter(d);

			// 2) Standard WPF CommandParameter (ButtonBase).
			if (parameter == null && d is ButtonBase bbForParam)
				parameter = bbForParam.CommandParameter;

			// 3) Fallback to event args.
			if (parameter == null)
				parameter = eventArgs;

			// Prevent double invocation when ButtonBase.Command is already wired to the same ICommand.
			if (d is ButtonBase bb && command != null && ReferenceEquals(bb.Command, command))
				return;

			// Re-entrancy guard.
			if (GetIsExecuting(d))
				return;

			SetIsExecuting(d, true);

			try
			{
				if (d is DreamineButton btn)
				{
					if (btn.MinimumGrade > 0 && btn.Grade < btn.MinimumGrade)
					{
						DreamineMessageBox.ShowAsync(
							"권한이 부족합니다.",
							"Access Denied",
							autoClick: MessageBoxResult.OK,
							autoClickDelaySeconds: 3);
						return;
					}

					if (btn.DataContext is ButtonData btnData)
						DreamineNavigationBar.NotifyButtonClicked(btnData);
				}

				if (command == null)
					return;

				// Execute the command when the parameter is accepted.
				if (command.CanExecute(parameter))
				{
					command.Execute(parameter);
				}

				eventArgs.Handled = true;
			}
			finally
			{
				SetIsExecuting(d, false);
			}
		}

		/** \brief Gradient top color. */
		/// <summary>
		/// \if KO
		/// <para>그라데이션 위쪽 배경 브러시 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the upper gradient-background brush property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty BackgroundTopProperty =
			DependencyProperty.Register(nameof(BackgroundTop), typeof(Brush), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Shine top color. */
		/// <summary>
		/// \if KO
		/// <para>위쪽 광택 브러시 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the upper shine-brush property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShineColorProperty =
			DependencyProperty.Register(nameof(ShineColor), typeof(Brush), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Shine bottom color. */
		/// <summary>
		/// \if KO
		/// <para>아래쪽 광택 브러시 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the lower shine-brush property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShineColorBottomProperty =
			DependencyProperty.Register(nameof(ShineColorBottom), typeof(Brush), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Gets/sets BackgroundTop. */
		/// <summary>
		/// \if KO
		/// <para>그라데이션 위쪽 배경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the upper gradient-background brush.</para>
		/// \endif
		/// </summary>
		public Brush BackgroundTop
		{
			get => (Brush)GetValue(BackgroundTopProperty);
			set => SetValue(BackgroundTopProperty, value);
		}

		/** \brief Gets/sets ShineColor. */
		/// <summary>
		/// \if KO
		/// <para>위쪽 광택 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the upper shine brush.</para>
		/// \endif
		/// </summary>
		public Brush ShineColor
		{
			get => (Brush)GetValue(ShineColorProperty);
			set => SetValue(ShineColorProperty, value);
		}

		/** \brief Gets/sets ShineColorBottom. */
		/// <summary>
		/// \if KO
		/// <para>아래쪽 광택 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the lower shine brush.</para>
		/// \endif
		/// </summary>
		public Brush ShineColorBottom
		{
			get => (Brush)GetValue(ShineColorBottomProperty);
			set => SetValue(ShineColorBottomProperty, value);
		}

		/** \brief Selection state. */
		/// <summary>
		/// \if KO
		/// <para>선택 상태 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the selection-state dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(DreamineButton), new PropertyMetadata(false));

		/** \brief Gets/sets IsSelected. */
		/// <summary>
		/// \if KO
		/// <para>버튼이 선택되었는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the button is selected.</para>
		/// \endif
		/// </summary>
		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		/** \brief Extended Focusable flag. */
		/// <summary>
		/// \if KO
		/// <para>확장 포커스 가능 상태 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the extended-focusability dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsFocusableExProperty =
			DependencyProperty.Register(nameof(IsFocusableEx), typeof(bool), typeof(DreamineButton), new PropertyMetadata(true));

		/** \brief Gets/sets IsFocusableEx. */
		/// <summary>
		/// \if KO
		/// <para>클릭 후 선택 상태를 변경할 수 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether clicking may change selection state.</para>
		/// \endif
		/// </summary>
		public bool IsFocusableEx
		{
			get => (bool)GetValue(IsFocusableExProperty);
			set => SetValue(IsFocusableExProperty, value);
		}

		/** \brief Focus restore target element. */
		/// <summary>
		/// \if KO
		/// <para>포커스 복귀 대상 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the focus-restore target dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty RestoreFocusTargetProperty =
			DependencyProperty.Register(nameof(RestoreFocusTarget), typeof(IInputElement), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Gets/sets RestoreFocusTarget. */
		/// <summary>
		/// \if KO
		/// <para>작업 후 포커스를 돌려보낼 입력 요소를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the input element to which focus should return after an action.</para>
		/// \endif
		/// </summary>
		public IInputElement? RestoreFocusTarget
		{
			get => (IInputElement?)GetValue(RestoreFocusTargetProperty);
			set => SetValue(RestoreFocusTargetProperty, value);
		}

		/** \brief Current user grade. */
		/// <summary>
		/// \if KO
		/// <para>현재 사용자 등급 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the current-user-grade dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty GradeProperty =
			DependencyProperty.Register(nameof(Grade), typeof(int), typeof(DreamineButton), new PropertyMetadata(0));

		/** \brief Gets/sets Grade. */
		/// <summary>
		/// \if KO
		/// <para>현재 사용자 등급을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the current user grade.</para>
		/// \endif
		/// </summary>
		public int Grade
		{
			get => (int)GetValue(GradeProperty);
			set => SetValue(GradeProperty, value);
		}

		/** \brief Minimum required grade. */
		/// <summary>
		/// \if KO
		/// <para>최소 요구 등급 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the minimum-required-grade dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty MinimumGradeProperty =
			DependencyProperty.Register(nameof(MinimumGrade), typeof(int), typeof(DreamineButton), new PropertyMetadata(0));

		/** \brief Gets/sets MinimumGrade. */
		/// <summary>
		/// \if KO
		/// <para>명령 실행에 필요한 최소 등급을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the minimum grade required to execute the command.</para>
		/// \endif
		/// </summary>
		public int MinimumGrade
		{
			get => (int)GetValue(MinimumGradeProperty);
			set => SetValue(MinimumGradeProperty, value);
		}

		/**
		 * \brief Enables solid background mode.
		 */
		/// <summary>
		/// \if KO
		/// <para>단색 배경 모드 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the solid-background-mode dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty UseSolidBackgroundProperty =
			DependencyProperty.Register(
				nameof(UseSolidBackground),
				typeof(bool),
				typeof(DreamineButton),
				new PropertyMetadata(false));

		/** \brief Gets/sets UseSolidBackground. */
		/// <summary>
		/// \if KO
		/// <para>단색 배경 모드 사용 여부를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether solid-background mode is enabled.</para>
		/// \endif
		/// </summary>
		public bool UseSolidBackground
		{
			get => (bool)GetValue(UseSolidBackgroundProperty);
			set => SetValue(UseSolidBackgroundProperty, value);
		}

		#endregion
	}
}
