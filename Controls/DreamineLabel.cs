// \file DreamineLabel.cs
// \brief Custom Label control used in VsLibrary.
// \details
// - Auto style merge (DreamineLabelStyle.xaml).
// - Attached command execution via configurable triggers.
// - Prevents duplicate handler attachment using internal hooked flag.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>전용 스타일과 이벤트 이름 기반 연결 명령을 제공하는 사용자 지정 레이블입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom label with a dedicated style and event-name-based attached commands.</para>
	/// \endif
	/// </summary>
	public class DreamineLabel : Label
	{
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 레이블 테마 리소스를 애플리케이션에 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges the label theme resources into the application.</para>
		/// \endif
		/// </summary>
		static DreamineLabel()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineLabel),
				new FrameworkPropertyMetadata(typeof(DreamineLabel)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineLabelStyle.xaml", UriKind.RelativeOrAbsolute);

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

		// =====================================================================
		// Attached Properties: Command / Parameter / Trigger
		// =====================================================================

		/// <summary>
		/// \if KO
		/// <para>구성한 트리거 이벤트에서 실행할 연결 명령 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached command executed for configured trigger events.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.RegisterAttached(
				"Command",
				typeof(ICommand),
				typeof(DreamineLabel),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \if KO
		/// <para>연결 명령에 전달할 선택적 매개변수 속성입니다. null이면 이벤트 데이터가 전달됩니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the optional attached command parameter; event data is passed when null.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineLabel),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>쉼표로 구분된 명령 트리거 이벤트 이름 목록 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the comma-separated list of command-trigger event names.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineLabel),
				new PropertyMetadata("PreviewMouseUp"));

		/// <summary>
		/// \if KO
		/// <para>내부 이벤트 처리기의 중복 연결을 방지하는 플래그 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal flag that prevents duplicate event-handler attachment.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsHandlersHooked",
				typeof(bool),
				typeof(DreamineLabel),
				new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>대상 개체에 명령 트리거 이벤트 이름 목록을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets command-trigger event names on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object on which to set the value.</para>
		/// \endif
		/// </param>
		/// <param name="value">
		/// \if KO
		/// <para>쉼표로 구분된 이벤트 이름입니다.</para>
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
		/// <para>대상 개체의 명령 트리거 이벤트 이름 목록을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets command-trigger event names from a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>쉼표로 구분된 이벤트 이름이며 값이 없으면 빈 문자열입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Comma-separated event names, or an empty string when absent.</para>
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
		public static string GetCommandTriggerName(DependencyObject obj)
			=> obj.GetValue(CommandTriggerNameProperty) as string ?? string.Empty;

		/// <summary>
		/// \if KO
		/// <para>대상 개체에 실행할 명령을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command to execute on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object on which to set the value.</para>
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
		public static void SetCommand(DependencyObject obj, ICommand value)
			=> obj.SetValue(CommandProperty, value);

		/// <summary>
		/// \if KO
		/// <para>대상 개체에 설정된 명령을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command configured on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 명령이며 없으면 null입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured command, or null when absent.</para>
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
		public static ICommand? GetCommand(DependencyObject obj)
			=> obj.GetValue(CommandProperty) as ICommand;

		/// <summary>
		/// \if KO
		/// <para>대상 개체에 명령 매개변수를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command parameter on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 설정할 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object on which to set the value.</para>
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
		/// <para>대상 개체에 설정된 명령 매개변수를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command parameter configured on a target object.</para>
		/// \endif
		/// </summary>
		/// <param name="obj">
		/// \if KO
		/// <para>값을 읽을 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 매개변수이며 없으면 null입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The configured parameter, or null when absent.</para>
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
		public static object? GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/// <summary>
		/// \if KO
		/// <para>명령 속성이 처음 설정될 때 지원되는 입력 이벤트 처리기를 한 번만 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attaches supported input-event handlers once when the command property is first set.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>명령 값이 변경된 종속성 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency object whose command changed.</para>
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
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not UIElement element)
				return;

			// \brief Prevent duplicate hooking
			bool hooked = (bool)(d.GetValue(IsHandlersHookedProperty) ?? false);
			if (hooked)
				return;

			d.SetValue(IsHandlersHookedProperty, true);

			// \brief Ensure hit-testable surface in common "label as button" usage
			if (d is Control c && c.Background == null)
				c.Background = Brushes.Transparent;

			// \brief Mouse up
			element.AddHandler(UIElement.PreviewMouseUpEvent,
				new MouseButtonEventHandler((s, args) => TryExecuteCommand(d, "PreviewMouseUp", args)),
				true);

			// \brief Double click (Label does not have MouseDoubleClick by default; Control.MouseDoubleClickEvent is routed)
			element.AddHandler(Control.MouseDoubleClickEvent,
				new MouseButtonEventHandler((s, args) => TryExecuteCommand(d, "MouseDoubleClick", args)),
				true);

			// \brief Touch up
			element.AddHandler(UIElement.TouchUpEvent,
				new EventHandler<TouchEventArgs>((s, args) => TryExecuteCommand(d, "TouchUp", args)));
		}

		/// <summary>
		/// \if KO
		/// <para>이벤트 이름이 구성된 목록에 있으면 명령 실행 가능 여부를 확인하고 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the command when the event name is configured and the command can run.</para>
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
			if (string.IsNullOrWhiteSpace(rawTrigger))
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
