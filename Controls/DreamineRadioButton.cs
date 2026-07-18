using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>전용 스타일과 여러 입력 이벤트용 연결 명령을 제공하는 사용자 지정 라디오 버튼입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom radio button with a dedicated style and attached commands for multiple input events.</para>
	/// \endif
	/// </summary>
	public class DreamineRadioButton : RadioButton
	{
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 라디오 버튼 테마 리소스를 애플리케이션에 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges the radio-button theme resources into the application.</para>
		/// \endif
		/// </summary>
		static DreamineRadioButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineRadioButton),
				new FrameworkPropertyMetadata(typeof(DreamineRadioButton)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineRadioButtonStyle.xaml", UriKind.RelativeOrAbsolute);

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
		/// <para>구성된 입력 이벤트에서 실행할 연결 명령 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached command executed for configured input events.</para>
		/// \endif
		/// </summary>
		public new static readonly DependencyProperty CommandProperty =
		DependencyProperty.RegisterAttached(
			"Command",
			typeof(ICommand),
			typeof(DreamineRadioButton),
			new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \if KO
		/// <para>명령에 전달할 연결 매개변수 속성입니다. 값이 없으면 이벤트 데이터가 사용됩니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached command parameter; event data is used when no value is provided.</para>
		/// \endif
		/// </summary>
		public new static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineRadioButton),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>명령 실행을 트리거할 쉼표 구분 이벤트 이름 목록 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the comma-separated event-name list that triggers command execution.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
		DependencyProperty.RegisterAttached(
			"CommandTriggerName",
			typeof(string),
			typeof(DreamineRadioButton),
			new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>대상 개체의 명령 트리거 이름을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets command-trigger names on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the value.</para>
		/// \endif
		/// </param>
		/// <param name="value">
		/// \if KO
		/// <para>쉼표 구분 이벤트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Comma-separated event names.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/// <summary>
		/// \if KO
		/// <para>대상 개체의 명령 트리거 이름을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets command-trigger names from a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 이벤트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured event names.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is null.</para>
		/// \endif
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>저장된 값이 문자열이 아니면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the stored value is not a string.</para>
		/// \endif
		/// </exception>
		public static string GetCommandTriggerName(DependencyObject obj)
			=> (string)obj.GetValue(CommandTriggerNameProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 개체의 명령을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the value.</para>
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
		/// <para><paramref name="obj"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

		/// <summary>
		/// \if KO
		/// <para>대상 개체의 명령을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command from a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 명령입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured command.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is null.</para>
		/// \endif
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>저장된 값이 명령이 아니면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the stored value is not a command.</para>
		/// \endif
		/// </exception>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/// <summary>
		/// \if KO
		/// <para>대상 개체의 명령 매개변수를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command parameter on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the value.</para>
		/// \endif
		/// </param>
		/// <param name="value">
		/// \if KO
		/// <para>명령 매개변수입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The command parameter.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="obj"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is null.</para>
		/// \endif
		/// </exception>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/// <summary>
		/// \if KO
		/// <para>대상 개체의 명령 매개변수를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command parameter from a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 개체입니다.</para>
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
		/// <para><paramref name="obj"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="obj"/> is null.</para>
		/// \endif
		/// </exception>
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/// <summary>
		/// \if KO
		/// <para>명령 값 변경 시 마우스, 키, 터치와 클릭 이벤트 처리기를 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attaches mouse, key, touch, and click event handlers when the command value changes.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>명령 값이 변경된 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose command changed.</para>
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
		/// <remarks>
		/// \if KO
		/// <para>현재 구현은 명령이 다시 변경될 때 처리기를 중복 연결할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>The current implementation can attach duplicate handlers when the command changes again.</para>
		/// \endif
		/// </remarks>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is UIElement element)
			{
				element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "PreviewMouseUp", e);
				}), true);

				element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "MouseDoubleClick", e);
				}), true);

				element.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "PreviewKeyUp", e);
				}), true);

				element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, e) =>
				{
					TryExecuteCommand(d, "TouchUp", e);
				}));

				element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "Click", e);
				}));
			}
		}

		/// <summary>
		/// \if KO
		/// <para>발생한 이벤트 이름이 구성된 트리거 목록과 일치하면 명령을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the command when the event name matches the configured trigger list.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>명령 설정을 보유한 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the command settings.</para>
		/// \endif
		/// </param>
		/// <param name="eventName">
		/// \if KO
		/// <para>발생한 이벤트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The name of the event that occurred.</para>
		/// \endif
		/// </param>
		/// <param name="eventArgs">
		/// \if KO
		/// <para>기본 명령 매개변수로 사용할 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Event data used as the default command parameter.</para>
		/// \endif
		/// </param>
		private static void TryExecuteCommand(DependencyObject d, string eventName, RoutedEventArgs eventArgs)
		{
			var rawTrigger = GetCommandTriggerName(d);
			if (string.IsNullOrEmpty(rawTrigger))
				return;

			var triggerList = rawTrigger.Split(',')
										.Select(x => x.Trim())
										.Where(x => !string.IsNullOrEmpty(x));

			if (!triggerList.Contains(eventName, StringComparer.OrdinalIgnoreCase))
				return;

			var command = GetCommand(d);
			var parameter = GetCommandParameter(d) ?? eventArgs;

			if (command?.CanExecute(parameter) == true)
				command.Execute(parameter);
		}
	}
}
