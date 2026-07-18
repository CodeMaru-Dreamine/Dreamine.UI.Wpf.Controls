using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>확장 상태 명령, 화살표 배치와 애니메이션 설정을 제공하는 사용자 지정 콘텐츠 컨테이너입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom content container with expansion-state commands, arrow placement, and animation settings.</para>
	/// \endif
	/// </summary>
	public class DreamineExpander : HeaderedContentControl
	{
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 Expander 테마 리소스를 한 번만 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges the expander theme resources once.</para>
		/// \endif
		/// </summary>
		static DreamineExpander()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineExpander),
				new FrameworkPropertyMetadata(typeof(DreamineExpander)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineExpanderStyle.xaml", UriKind.RelativeOrAbsolute);

			if (Application.Current != null)
			{
				bool alreadyAdded = Application.Current.Resources.MergedDictionaries
					.OfType<ResourceDictionary>()
					.Any(x => x.Source != null && x.Source.Equals(uri));

				if (!alreadyAdded)
				{
					var dict = new ResourceDictionary { Source = uri };
					Application.Current.Resources.MergedDictionaries.Add(dict);
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>콘텐츠가 현재 확장되어 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the content is currently expanded.</para>
		/// \endif
		/// </summary>
		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="IsExpanded"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="IsExpanded"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(DreamineExpander),
				new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsExpandedChanged));

		/// <summary>
		/// \if KO
		/// <para>확장 상태 변경 시 명령을 실행하거나 확장 아이콘이 없으면 접힘을 취소합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the state-change command or cancels collapse when no expand icon is shown.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>상태가 변경된 Expander입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The expander whose state changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 값과 새 값을 포함한 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the old and new values.</para>
		/// \endif
		/// </param>
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>새 값이 부울이 아니면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the new value is not Boolean.</para>
		/// \endif
		/// </exception>
		private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineExpander expander)
			{
				if (expander.ExpandChangedCommand?.CanExecute(null) == true)
				{
					expander.ExpandChangedCommand.Execute(null);
				}
				else
				{
					if ((bool)expander.ShowExpandIcon == false && (bool)e.NewValue == false)
						expander.IsExpanded = true;
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>확장 상태가 바뀔 때 실행할 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the command executed when expansion state changes.</para>
		/// \endif
		/// </summary>
		public ICommand ExpandChangedCommand
		{
			get => (ICommand)GetValue(ExpandChangedCommandProperty);
			set => SetValue(ExpandChangedCommandProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="ExpandChangedCommand"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ExpandChangedCommand"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ExpandChangedCommandProperty =
			DependencyProperty.Register(nameof(ExpandChangedCommand), typeof(ICommand), typeof(DreamineExpander));

		/// <summary>
		/// \if KO
		/// <para>확장하거나 접을 때 애니메이션을 사용할지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether animation is used while expanding or collapsing.</para>
		/// \endif
		/// </summary>
		public bool UseExpandAnimation
		{
			get => (bool)GetValue(UseExpandAnimationProperty);
			set => SetValue(UseExpandAnimationProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="UseExpandAnimation"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="UseExpandAnimation"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty UseExpandAnimationProperty =
			DependencyProperty.Register(nameof(UseExpandAnimation), typeof(bool), typeof(DreamineExpander), new PropertyMetadata(true));

		/// <summary>
		/// \if KO
		/// <para>확장 화살표 아이콘의 위치를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the expand-arrow icon position.</para>
		/// \endif
		/// </summary>
		public ExpanderArrowPlacement ArrowPlacement
		{
			get => (ExpanderArrowPlacement)GetValue(ArrowPlacementProperty);
			set => SetValue(ArrowPlacementProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="ArrowPlacement"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ArrowPlacement"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ArrowPlacementProperty =
			DependencyProperty.Register(nameof(ArrowPlacement), typeof(ExpanderArrowPlacement), typeof(DreamineExpander),
				new PropertyMetadata(ExpanderArrowPlacement.Left));

		/// <summary>
		/// \if KO
		/// <para>헤더 글꼴 크기를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the header font size.</para>
		/// \endif
		/// </summary>
		public double HeaderFontSize
		{
			get { return (double)GetValue(HeaderFontSizeProperty); }
			set { SetValue(HeaderFontSizeProperty, value); }
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="HeaderFontSize"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="HeaderFontSize"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty HeaderFontSizeProperty =
			DependencyProperty.Register("HeaderFontSize", typeof(double), typeof(DreamineExpander), new PropertyMetadata(SystemFonts.MessageFontSize));

		/// <summary>
		/// \if KO
		/// <para>헤더 글꼴 두께를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the header font weight.</para>
		/// \endif
		/// </summary>
		public FontWeight HeaderFontWeight
		{
			get { return (FontWeight)GetValue(HeaderFontWeightProperty); }
			set { SetValue(HeaderFontWeightProperty, value); }
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="HeaderFontWeight"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="HeaderFontWeight"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty HeaderFontWeightProperty =
			DependencyProperty.Register("HeaderFontWeight", typeof(FontWeight), typeof(DreamineExpander), new PropertyMetadata(FontWeights.Normal));

		/// <summary>
		/// \if KO
		/// <para>헤더 텍스트의 전경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the header-text foreground brush.</para>
		/// \endif
		/// </summary>
		public Brush HeaderForeground
		{
			get { return (Brush)GetValue(HeaderForegroundProperty); }
			set { SetValue(HeaderForegroundProperty, value); }
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="HeaderForeground"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="HeaderForeground"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty HeaderForegroundProperty =
			DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(DreamineExpander), new PropertyMetadata(SystemColors.ControlTextBrush));

		/// <summary>
		/// \if KO
		/// <para>확장·접기 아이콘을 표시할지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the expand-collapse icon is shown.</para>
		/// \endif
		/// </summary>
		public bool ShowExpandIcon
		{
			get => (bool)GetValue(ShowExpandIconProperty);
			set => SetValue(ShowExpandIconProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="ShowExpandIcon"/> 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ShowExpandIcon"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ShowExpandIconProperty =
			DependencyProperty.Register(nameof(ShowExpandIcon), typeof(bool), typeof(DreamineExpander),
				new PropertyMetadata(true));
	}

	/// <summary>
	/// \if KO
	/// <para><see cref="DreamineExpander"/> 헤더에서 확장 화살표의 배치를 지정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Specifies placement of the expand arrow in a <see cref="DreamineExpander"/> header.</para>
	/// \endif
	/// </summary>
	public enum ExpanderArrowPlacement
	{
		/// <summary>
		/// \if KO
		/// <para>헤더 왼쪽에 화살표를 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays the arrow on the left side of the header.</para>
		/// \endif
		/// </summary>
		Left,

		/// <summary>
		/// \if KO
		/// <para>헤더 오른쪽에 화살표를 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays the arrow on the right side of the header.</para>
		/// \endif
		/// </summary>
		Right
	}
}
