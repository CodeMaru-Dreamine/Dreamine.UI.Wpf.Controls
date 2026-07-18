// \file DreamineCheckBox.cs
// \brief Custom CheckBox control for VsLibrary. Supports attached Command/Parameter/Trigger and Enter/Space keyboard behavior.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>전용 스타일, 여러 입력 이벤트용 연결 명령과 Enter 키 토글을 제공하는 사용자 지정 체크박스입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom check box with a dedicated style, attached commands for multiple input events, and Enter-key toggling.</para>
	/// \endif
	/// </summary>
	public class DreamineCheckBox : CheckBox
	{
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 체크박스 테마 리소스를 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges the check-box theme resources.</para>
		/// \endif
		/// </summary>
		static DreamineCheckBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineCheckBox),
				new FrameworkPropertyMetadata(typeof(DreamineCheckBox)));

			try
			{
				var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineCheckBoxStyle.xaml", UriKind.RelativeOrAbsolute);

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

		#region Attached DPs (Command/Parameter/Trigger)

		/// <summary>
		/// \if KO
		/// <para>이벤트 처리기의 중복 연결을 방지하는 내부 플래그 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal flag that prevents duplicate event-handler attachment.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached("IsHandlersHooked", typeof(bool), typeof(DreamineCheckBox),
				new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>처리기 연결 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether handlers have been attached.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>값을 읽을 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object from which to read the value.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>처리기가 연결되어 있으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when handlers are attached.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="d"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="d"/> is null.</para>
		/// \endif
		/// </exception>
		private static bool GetIsHandlersHooked(DependencyObject d) => (bool)d.GetValue(IsHandlersHookedProperty);

		/// <summary>
		/// \if KO
		/// <para>처리기 연결 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether handlers have been attached.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>값을 설정할 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object on which to set the value.</para>
		/// \endif
		/// </param>
		/// <param name="v">
		/// \if KO
		/// <para>처리기 연결 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The handler-attachment state.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="d"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="d"/> is null.</para>
		/// \endif
		/// </exception>
		private static void SetIsHandlersHooked(DependencyObject d, bool v) => d.SetValue(IsHandlersHookedProperty, v);

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
				typeof(DreamineCheckBox),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \if KO
		/// <para>명령에 전달할 연결 매개변수 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached command-parameter property.</para>
		/// \endif
		/// </summary>
		public new static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineCheckBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>쉼표로 구분된 명령 트리거 이벤트 이름 목록 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the comma-separated command-trigger event-name property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineCheckBox),
				new PropertyMetadata("Click"));

		/// <summary>
		/// \if KO
		/// <para>명령 트리거 이벤트 이름을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets command-trigger event names.</para>
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
		/// <para>명령 트리거 이벤트 이름을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets command-trigger event names.</para>
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
		/// <para>실행할 명령을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command to execute.</para>
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
		public static void SetCommand(DependencyObject obj, ICommand value)
			=> obj.SetValue(CommandProperty, value);

		/// <summary>
		/// \if KO
		/// <para>설정된 명령을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the configured command.</para>
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
		public static ICommand GetCommand(DependencyObject obj)
			=> (ICommand)obj.GetValue(CommandProperty);

		/// <summary>
		/// \if KO
		/// <para>명령 매개변수를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command parameter.</para>
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
		/// <para>명령 매개변수를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command parameter.</para>
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

		#endregion

		#region Hook event handlers

		/// <summary>
		/// \if KO
		/// <para>연결 명령 변경 시 마우스, 키보드, 터치와 클릭 처리기를 한 번만 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attaches mouse, keyboard, touch, and click handlers once when the attached command changes.</para>
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
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not UIElement element)
				return;

			// \brief 중복 핸들러 등록 방지
			if (GetIsHandlersHooked(d))
				return;

			// \brief 마우스
			element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "PreviewMouseUp", ev);
			}), true);

			element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "MouseDoubleClick", ev);
			}), true);

			// \brief 키보드(Down): Enter/Return은 기본 CheckBox에서 토글이 아닐 수 있으므로 직접 토글 처리
			element.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, ev) =>
			{
				// \brief 주 키 엔터는 Return으로 들어오는 경우가 많다.
				if (ev.Key == Key.Enter || ev.Key == Key.Return)
				{
					// \brief Enter/Return 토글 강제
					if (d is ToggleButton tb)
					{
						bool current = tb.IsChecked == true;
						tb.IsChecked = !current;
					}

					// \brief 트리거 이름은 "PreviewKeyDown"으로도 쓸 수 있게 한다.
					// \note 기존 예제는 PreviewKeyUp을 많이 쓰므로 둘 다 호출.
					TryExecuteCommand(d, "PreviewKeyDown", ev);
					TryExecuteCommand(d, "PreviewKeyUp", ev);

					// \brief Enter가 DefaultButton으로 새는게 싫으면 막아라.
					ev.Handled = true;
				}
				else if (ev.Key == Key.Space)
				{
					// \brief Space는 WPF 기본 토글이 정상 동작한다.
					// \note 여기서 굳이 토글하지 않는다.
					TryExecuteCommand(d, "PreviewKeyDown", ev);
					// ev.Handled = false; // 기본 토글에 맡김
				}
			}), true);

			// \brief 키보드(Up): Space/Enter/Return 지원
			element.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler((s, ev) =>
			{
				if (ev.Key != Key.Enter && ev.Key != Key.Return && ev.Key != Key.Space)
					return;

				TryExecuteCommand(d, "PreviewKeyUp", ev);

				// \brief Up에서 막고 싶으면 활성화
				// ev.Handled = true;
			}), true);

			// \brief 터치
			element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, ev) =>
			{
				TryExecuteCommand(d, "TouchUp", ev);
			}));

			// \brief Click
			element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "Click", ev);
			}));

			SetIsHandlersHooked(d, true);
		}

		#endregion

		#region Execute command

		/// <summary>
		/// \if KO
		/// <para>발생한 이벤트 이름이 트리거 목록과 일치하면 연결 명령을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the attached command when the event name matches the trigger list.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>명령 설정을 보유한 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns command settings.</para>
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

			// \brief null/empty면 실행 안 함 (명시적으로 "Click"을 기본값으로 줬음)
			if (string.IsNullOrWhiteSpace(rawTrigger))
				return;

			var triggers = rawTrigger
				.Split(',')
				.Select(x => x.Trim())
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToArray();

			if (triggers.Length > 0 &&
				!triggers.Contains(eventName, StringComparer.OrdinalIgnoreCase))
				return;

			var command = GetCommand(d);
			if (command == null)
				return;

			var parameter = GetCommandParameter(d) ?? eventArgs;

			if (command.CanExecute(parameter))
				command.Execute(parameter);
		}

		#endregion
	}
}
