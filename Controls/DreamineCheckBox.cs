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
	/// \class DreamineCheckBox
	/// \brief Custom CheckBox control for VsLibrary.
	///
	/// \details
	/// - Attached Command/CommandParameter/CommandTriggerName 지원
	/// - PreviewMouseUp/MouseDoubleClick/PreviewKeyDown/PreviewKeyUp/TouchUp/Click 트리거 지원
	/// - CheckBox 표준( Space 토글 ) + Enter/Return 토글을 함께 지원 (Enter는 기본 WPF에서 토글이 아님)
	/// - /UiComponent/Styles/DreamineCheckBoxStyle.xaml 자동 Merge
	/// </summary>
	public class DreamineCheckBox : CheckBox
	{
		/// <summary>
		/// \brief Static ctor: style dictionary merge and default style key override
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

		/// <summary> \brief Prevents duplicate event handler hooking. </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached("IsHandlersHooked", typeof(bool), typeof(DreamineCheckBox),
				new PropertyMetadata(false));

		/// <summary> \brief Gets IsHandlersHooked attached property. </summary>
		private static bool GetIsHandlersHooked(DependencyObject d) => (bool)d.GetValue(IsHandlersHookedProperty);

		/// <summary> \brief Sets IsHandlersHooked attached property. </summary>
		private static void SetIsHandlersHooked(DependencyObject d, bool v) => d.SetValue(IsHandlersHookedProperty, v);

		/// <summary>
		/// \brief Attached property: Command
		/// \details SetCommand / GetCommand 로 사용한다. (인스턴스 Command와 혼동 주의)
		/// </summary>
		public new static readonly DependencyProperty CommandProperty =
			DependencyProperty.RegisterAttached(
				"Command",
				typeof(ICommand),
				typeof(DreamineCheckBox),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \brief Attached property: CommandParameter
		/// </summary>
		public new static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineCheckBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \brief Attached property: CommandTriggerName
		/// \details Comma-separated triggers. Example: "Click,PreviewMouseUp,PreviewKeyUp"
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineCheckBox),
				new PropertyMetadata("Click"));

		/// <summary> \brief Sets CommandTriggerName. </summary>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/// <summary> \brief Gets CommandTriggerName. </summary>
		public static string GetCommandTriggerName(DependencyObject obj)
			=> (string)obj.GetValue(CommandTriggerNameProperty);

		/// <summary> \brief Sets Command. </summary>
		public static void SetCommand(DependencyObject obj, ICommand value)
			=> obj.SetValue(CommandProperty, value);

		/// <summary> \brief Gets Command. </summary>
		public static ICommand GetCommand(DependencyObject obj)
			=> (ICommand)obj.GetValue(CommandProperty);

		/// <summary> \brief Sets CommandParameter. </summary>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/// <summary> \brief Gets CommandParameter. </summary>
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		#endregion

		#region Hook event handlers

		/// <summary>
		/// \brief Hooks handlers once when attached Command changes.
		/// \details
		/// - PreviewMouseUp
		/// - MouseDoubleClick
		/// - PreviewKeyDown (Enter/Return 토글 보장)
		/// - PreviewKeyUp (Enter/Return/Space)
		/// - TouchUp
		/// - Click
		/// </summary>
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
		/// \brief Executes attached command if trigger matches.
		/// \details
		/// - CommandTriggerName은 콤마로 분리하여 비교한다.
		/// - CommandParameter가 없으면 eventArgs를 전달한다.
		/// </summary>
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
