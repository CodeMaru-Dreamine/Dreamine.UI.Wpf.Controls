using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>시·분·초를 입력하고 선택된 구간을 위아래로 조절하는 시간 스피너 컨트롤입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Represents a time spinner for entering hours, minutes, and seconds and adjusting the active segment.</para>
	/// \endif
	/// </summary>
	public class DreamineTimeSpinner : Control
	{
		/// <summary>
		/// \if KO
		/// <para>시간 입력 텍스트 상자 템플릿 파트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the template-part name for the hour input text box.</para>
		/// \endif
		/// </summary>
		private const string PART_HourBox = "PART_HourBox";

		/// <summary>
		/// \if KO
		/// <para>분 입력 텍스트 상자 템플릿 파트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the template-part name for the minute input text box.</para>
		/// \endif
		/// </summary>
		private const string PART_MinBox = "PART_MinBox";

		/// <summary>
		/// \if KO
		/// <para>초 입력 텍스트 상자 템플릿 파트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the template-part name for the second input text box.</para>
		/// \endif
		/// </summary>
		private const string PART_SecBox = "PART_SecBox";

		/// <summary>
		/// \if KO
		/// <para>증가 버튼 템플릿 파트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the template-part name for the increment button.</para>
		/// \endif
		/// </summary>
		private const string PART_UpButton = "PART_UpButton";

		/// <summary>
		/// \if KO
		/// <para>감소 버튼 템플릿 파트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the template-part name for the decrement button.</para>
		/// \endif
		/// </summary>
		private const string PART_DownButton = "PART_DownButton";

		/// <summary>
		/// \if KO
		/// <para>적용된 시간 입력 텍스트 상자를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the applied hour-input text-box part.</para>
		/// \endif
		/// </summary>
		private TextBox? _hourBox;

		/// <summary>
		/// \if KO
		/// <para>적용된 분 입력 텍스트 상자를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the applied minute-input text-box part.</para>
		/// \endif
		/// </summary>
		private TextBox? _minBox;

		/// <summary>
		/// \if KO
		/// <para>적용된 초 입력 텍스트 상자를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the applied second-input text-box part.</para>
		/// \endif
		/// </summary>
		private TextBox? _secBox;

		/// <summary>
		/// \if KO
		/// <para>적용된 증가 버튼 파트를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the applied increment-button part.</para>
		/// \endif
		/// </summary>
		private ButtonBase? _upButton;

		/// <summary>
		/// \if KO
		/// <para>적용된 감소 버튼 파트를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the applied decrement-button part.</para>
		/// \endif
		/// </summary>
		private ButtonBase? _downButton;

		/// <summary>
		/// \if KO
		/// <para>현재 편집 중인 시간 구성 요소를 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the time component currently being edited.</para>
		/// \endif
		/// </summary>
		private enum eFocusedBox
		{
			/// <summary>
			/// \if KO
			/// <para>시간 입력 구간을 나타냅니다.</para>
			/// \endif
			/// \if EN
			/// <para>Represents the hour input segment.</para>
			/// \endif
			/// </summary>
			Hour,

			/// <summary>
			/// \if KO
			/// <para>분 입력 구간을 나타냅니다.</para>
			/// \endif
			/// \if EN
			/// <para>Represents the minute input segment.</para>
			/// \endif
			/// </summary>
			Minute,

			/// <summary>
			/// \if KO
			/// <para>초 입력 구간을 나타냅니다.</para>
			/// \endif
			/// \if EN
			/// <para>Represents the second input segment.</para>
			/// \endif
			/// </summary>
			Second
		}

		/// <summary>
		/// \if KO
		/// <para>현재 활성화된 시간 입력 구간을 보관하며 기본값은 분입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Tracks the active time-input segment, defaulting to minutes.</para>
		/// \endif
		/// </summary>
		private eFocusedBox _activeBox = eFocusedBox.Minute;

		/// <summary>
		/// \if KO
		/// <para>현재 24시간 형식의 시간 값을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the current time value in 24-hour form.</para>
		/// \endif
		/// </summary>
		public TimeSpan Time
		{
			get => (TimeSpan)GetValue(TimeProperty);
			set => SetValue(TimeProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>양방향 바인딩되는 <see cref="Time"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the two-way-bindable <see cref="Time"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty TimeProperty =
			DependencyProperty.Register(
				nameof(Time),
				typeof(TimeSpan),
				typeof(DreamineTimeSpinner),
				new FrameworkPropertyMetadata(default(TimeSpan), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeChanged));

		/// <summary>
		/// \if KO
		/// <para>시간 값 변경을 템플릿의 시·분·초 입력 상자에 반영합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Synchronizes template input boxes when the time value changes.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>시간이 변경된 종속성 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object whose time changed.</para>
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
		private static void OnTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var spinner = d as DreamineTimeSpinner;
			spinner?.UpdateTextBoxes();
		}

		/// <summary>
		/// \if KO
		/// <para>활성 시간 구간을 증가시키는 라우트 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the routed command that increments the active time segment.</para>
		/// \endif
		/// </summary>
		public static RoutedCommand UpCommand { get; set; } = new(nameof(UpCommand), typeof(DreamineTimeSpinner));

		/// <summary>
		/// \if KO
		/// <para>활성 시간 구간을 감소시키는 라우트 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the routed command that decrements the active time segment.</para>
		/// \endif
		/// </summary>
		public static RoutedCommand DownCommand { get; set; } = new(nameof(DownCommand), typeof(DreamineTimeSpinner));

		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 등록하고 시간 스피너 테마 리소스를 애플리케이션에 한 번 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Registers the default style key and merges the time-spinner theme resource into the application once.</para>
		/// \endif
		/// </summary>
		static DreamineTimeSpinner()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(DreamineTimeSpinner),
				new FrameworkPropertyMetadata(typeof(DreamineTimeSpinner)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineTimeSpinnerStyle.xaml", UriKind.RelativeOrAbsolute);

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
		/// <para>새 인스턴스를 만들고 증가·감소 명령 바인딩을 등록합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a new instance and registers increment and decrement command bindings.</para>
		/// \endif
		/// </summary>
		public DreamineTimeSpinner()
		{
			CommandBindings.Add(new CommandBinding(UpCommand, OnUpExecuted));
			CommandBindings.Add(new CommandBinding(DownCommand, OnDownExecuted));
		}

		/// <summary>
		/// \if KO
		/// <para>템플릿 파트를 찾아 포커스·키 이벤트와 증가·감소 명령을 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Resolves template parts and connects focus and key events plus increment and decrement commands.</para>
		/// \endif
		/// </summary>
		/// <remarks>
		/// \if KO
		/// <para>템플릿을 다시 적용할 때 이전 파트의 이벤트를 해제하지 않는 현재 동작을 유지합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Preserves the current behavior of not detaching events from previous parts when the template is reapplied.</para>
		/// \endif
		/// </remarks>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_hourBox = GetTemplateChild(PART_HourBox) as TextBox;
			_minBox = GetTemplateChild(PART_MinBox) as TextBox;
			_secBox = GetTemplateChild(PART_SecBox) as TextBox;
			_upButton = GetTemplateChild(PART_UpButton) as ButtonBase;
			_downButton = GetTemplateChild(PART_DownButton) as ButtonBase;

			if (_hourBox != null)
			{
				_hourBox.GotFocus += (_, __) => _activeBox = eFocusedBox.Hour;
				_hourBox.LostFocus += HourBox_LostFocus;
				_hourBox.PreviewKeyDown += HourBox_PreviewKeyDown;
			}
			if (_minBox != null)
			{
				_minBox.GotFocus += (_, __) => _activeBox = eFocusedBox.Minute;
				_minBox.LostFocus += MinBox_LostFocus;
				_minBox.PreviewKeyDown += MinBox_PreviewKeyDown;
			}
			if (_secBox != null)
			{
				_secBox.GotFocus += (_, __) => _activeBox = eFocusedBox.Second;
				_secBox.LostFocus += SecBox_LostFocus;
				_secBox.PreviewKeyDown += SecBox_PreviewKeyDown;
			}

			if (_upButton != null)
				_upButton.Command = UpCommand;
			if (_downButton != null)
				_downButton.Command = DownCommand;

			UpdateTextBoxes();
		}

		/// <summary>
		/// \if KO
		/// <para>시간 입력 상자가 포커스를 잃으면 세 입력값을 <see cref="Time"/>에 반영합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies all three input values to <see cref="Time"/> when the hour box loses focus.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>포커스 손실 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Focus-loss event data.</para>
		/// \endif
		/// </param>
		private void HourBox_LostFocus(object sender, RoutedEventArgs e) => TryUpdateTimeFromBoxes();

		/// <summary>
		/// \if KO
		/// <para>분 입력 상자가 포커스를 잃으면 세 입력값을 <see cref="Time"/>에 반영합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies all three input values to <see cref="Time"/> when the minute box loses focus.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>포커스 손실 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Focus-loss event data.</para>
		/// \endif
		/// </param>
		private void MinBox_LostFocus(object sender, RoutedEventArgs e) => TryUpdateTimeFromBoxes();

		/// <summary>
		/// \if KO
		/// <para>초 입력 상자가 포커스를 잃으면 세 입력값을 <see cref="Time"/>에 반영합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies all three input values to <see cref="Time"/> when the second box loses focus.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>포커스 손실 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Focus-loss event data.</para>
		/// \endif
		/// </param>
		private void SecBox_LostFocus(object sender, RoutedEventArgs e) => TryUpdateTimeFromBoxes();

		/// <summary>
		/// \if KO
		/// <para>시간 입력 상자에서 Enter 키를 누르면 현재 입력을 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies the current input when Enter is pressed in the hour box.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>누른 키 정보입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Data describing the pressed key.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private void HourBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TryUpdateTimeFromBoxes();
		}

		/// <summary>
		/// \if KO
		/// <para>분 입력 상자에서 Enter 키를 누르면 현재 입력을 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies the current input when Enter is pressed in the minute box.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>누른 키 정보입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Data describing the pressed key.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private void MinBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TryUpdateTimeFromBoxes();
		}

		/// <summary>
		/// \if KO
		/// <para>초 입력 상자에서 Enter 키를 누르면 현재 입력을 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies the current input when Enter is pressed in the second box.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>누른 키 정보입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Data describing the pressed key.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private void SecBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TryUpdateTimeFromBoxes();
		}

		/// <summary>
		/// \if KO
		/// <para>세 입력값을 해석하고 유효 범위로 제한하여 <see cref="Time"/>과 표시값을 갱신합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Parses the three inputs, clamps them to valid ranges, and updates <see cref="Time"/> and displayed values.</para>
		/// \endif
		/// </summary>
		/// <remarks>
		/// \if KO
		/// <para>해석할 수 없는 값은 0으로 처리하며 템플릿 파트가 없으면 아무 작업도 하지 않습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Unparseable values become zero; no action is taken when template parts are unavailable.</para>
		/// \endif
		/// </remarks>
		private void TryUpdateTimeFromBoxes()
		{
			if (_hourBox == null || _minBox == null || _secBox == null) return;

			int h = Clamp(Parse(_hourBox.Text), 0, 23);
			int m = Clamp(Parse(_minBox.Text), 0, 59);
			int s = Clamp(Parse(_secBox.Text), 0, 59);

			Time = new TimeSpan(h, m, s);

			UpdateTextBoxes();
		}

		/// <summary>
		/// \if KO
		/// <para>문자열을 정수로 해석하며 실패하면 0을 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Parses a string as an integer and returns zero on failure.</para>
		/// \endif
		/// </summary>
		/// <param name="s">
		/// \if KO
		/// <para>해석할 문자열입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The string to parse.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>해석한 정수이며 입력이 없거나 유효하지 않으면 0입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The parsed integer, or zero when the input is null or invalid.</para>
		/// \endif
		/// </returns>
		private int Parse(string? s)
		{
			return int.TryParse(s, out var v) ? v : 0;
		}

		/// <summary>
		/// \if KO
		/// <para>정수를 지정한 포함 범위로 제한합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Clamps an integer to the specified inclusive range.</para>
		/// \endif
		/// </summary>
		/// <param name="value">
		/// \if KO
		/// <para>제한할 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The value to clamp.</para>
		/// \endif
		/// </param>
		/// <param name="min">
		/// \if KO
		/// <para>허용할 최솟값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The minimum allowed value.</para>
		/// \endif
		/// </param>
		/// <param name="max">
		/// \if KO
		/// <para>허용할 최댓값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The maximum allowed value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para><paramref name="min"/>과 <paramref name="max"/> 사이로 제한된 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The value clamped between <paramref name="min"/> and <paramref name="max"/>.</para>
		/// \endif
		/// </returns>
		private int Clamp(int value, int min, int max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		/// <summary>
		/// \if KO
		/// <para>활성 구간을 한 단위 증가시키고 상위 구간으로 올림하여 24시간 범위에서 순환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Increments the active segment by one, carrying into higher segments and wrapping within a 24-hour range.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>명령을 실행한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that executed the command.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 명령 실행 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Routed-command execution data.</para>
		/// \endif
		/// </param>
		private void OnUpExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			switch (_activeBox)
			{
				case eFocusedBox.Hour:
					Time = new TimeSpan((Time.Hours + 1) % 24, Time.Minutes, Time.Seconds);
					break;
				case eFocusedBox.Minute:
					if (Time.Minutes == 59)
					{
						int newHour = (Time.Hours + 1) % 24;
						Time = new TimeSpan(newHour, 0, Time.Seconds);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes + 1, Time.Seconds);
					}
					break;
				case eFocusedBox.Second:
					if (Time.Seconds == 59)
					{
						int newMinute = Time.Minutes + 1;
						int newHour = Time.Hours;
						if (newMinute == 60)
						{
							newMinute = 0;
							newHour = (newHour + 1) % 24;
						}
						Time = new TimeSpan(newHour, newMinute, 0);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes, Time.Seconds + 1);
					}
					break;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>활성 구간을 한 단위 감소시키고 상위 구간에서 빌려와 24시간 범위에서 순환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Decrements the active segment by one, borrowing from higher segments and wrapping within a 24-hour range.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>명령을 실행한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that executed the command.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 명령 실행 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Routed-command execution data.</para>
		/// \endif
		/// </param>
		private void OnDownExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			switch (_activeBox)
			{
				case eFocusedBox.Hour:
					Time = new TimeSpan((Time.Hours + 23) % 24, Time.Minutes, Time.Seconds);
					break;
				case eFocusedBox.Minute:
					if (Time.Minutes == 0)
					{
						int newHour = (Time.Hours + 23) % 24;
						Time = new TimeSpan(newHour, 59, Time.Seconds);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes - 1, Time.Seconds);
					}
					break;
				case eFocusedBox.Second:
					if (Time.Seconds == 0)
					{
						int newMinute = Time.Minutes;
						int newHour = Time.Hours;
						if (newMinute == 0)
						{
							newMinute = 59;
							newHour = (newHour + 23) % 24;
						}
						else
						{
							newMinute = newMinute - 1;
						}
						Time = new TimeSpan(newHour, newMinute, 59);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes, Time.Seconds - 1);
					}
					break;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>존재하는 템플릿 입력 상자에 현재 시간을 두 자리 문자열로 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays the current time as two-digit strings in available template input boxes.</para>
		/// \endif
		/// </summary>
		private void UpdateTextBoxes()
		{
			if (_hourBox != null)
				_hourBox.Text = Time.Hours.ToString("D2");
			if (_minBox != null)
				_minBox.Text = Time.Minutes.ToString("D2");
			if (_secBox != null)
				_secBox.Text = Time.Seconds.ToString("D2");
		}
	}
}
