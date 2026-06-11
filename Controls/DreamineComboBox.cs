// =====================================================================
// \file DreamineComboBox.cs
// \brief MVVM-style attached command trigger ComboBox
// \details
//  - Supports triggers: PreviewMouseUp, MouseDoubleClick, PreviewKeyDown, PreviewKeyUp, TouchUp,
//    Click, SelectionChanged, DropDownClosed
//  - Prevents duplicated handler hookup by using attached flags
//  - Unhooks DropDownClosed on Unloaded to avoid leaks / duplicates
//  - If CommandParameter is null, it passes a rich context object including Sender + EventArgs
// \date 2026-02-10
// =====================================================================

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineComboBox
	/// \brief ComboBox with MVVM-style attached command trigger support.
	/// </summary>
	public class DreamineComboBox : ComboBox
	{
		#region Command Context

		/// <summary>
		/// \class DreamineComboBoxCommandContext
		/// \brief Command execution context passed to ICommand when CommandParameter is not provided.
		/// </summary>
		public sealed class DreamineComboBoxCommandContext
		{
			/// <summary> \brief Trigger event name. </summary>
			public string EventName { get; }

			/// <summary> \brief Original sender (usually ComboBox). </summary>
			public object? Sender { get; }

			/// <summary> \brief Original event args. </summary>
			public object? EventArgs { get; }

			/// <summary> \brief ComboBox instance if Sender is ComboBox or d is ComboBox. </summary>
			public ComboBox? ComboBox { get; }

			/// <summary> \brief Current SelectedItem snapshot. </summary>
			public object? SelectedItem { get; }

			/// <summary> \brief Current SelectedValue snapshot. </summary>
			public object? SelectedValue { get; }

			/// <summary> \brief Current SelectedIndex snapshot. </summary>
			public int SelectedIndex { get; }

			/// <summary>
			/// \brief Create context from sender/d and args.
			/// </summary>
			/// <param name="eventName"> \brief Trigger event name </param>
			/// <param name="sender"> \brief Original sender </param>
			/// <param name="args"> \brief Original args </param>
			/// <param name="combo"> \brief ComboBox instance </param>
			public DreamineComboBoxCommandContext(string eventName, object? sender, object? args, ComboBox? combo)
			{
				EventName = eventName ?? string.Empty;
				Sender = sender;
				EventArgs = args;
				ComboBox = combo;

				if (combo != null)
				{
					SelectedItem = combo.SelectedItem;
					SelectedValue = combo.SelectedValue;
					SelectedIndex = combo.SelectedIndex;
				}
				else
				{
					SelectedItem = null;
					SelectedValue = null;
					SelectedIndex = -1;
				}
			}

			/// <summary> \brief Debug-friendly string. </summary>
			public override string ToString()
				=> $"Event={EventName}, SelectedItem={SelectedItem ?? "(null)"}, SelectedIndex={SelectedIndex}";
		}

		#endregion

		#region Attached Flags (Private)

		/// <summary> \brief Routed/Handler 훅 중복 방지 플래그 </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsHandlersHooked",
				typeof(bool),
				typeof(DreamineComboBox),
				new PropertyMetadata(false));

		/// <summary> \brief DropDownClosed 구독 중복 방지 플래그 </summary>
		private static readonly DependencyProperty IsDropDownClosedHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsDropDownClosedHooked",
				typeof(bool),
				typeof(DreamineComboBox),
				new PropertyMetadata(false));

		/// <summary> \brief Unloaded 핸들러 구독 중복 방지 플래그 </summary>
		private static readonly DependencyProperty IsUnloadedHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsUnloadedHooked",
				typeof(bool),
				typeof(DreamineComboBox),
				new PropertyMetadata(false));

		/// <summary> \brief DropDownClosed 핸들러 인스턴스 저장(해제용) </summary>
		private static readonly DependencyProperty DropDownClosedHandlerProperty =
			DependencyProperty.RegisterAttached(
				"DropDownClosedHandler",
				typeof(EventHandler),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary> \brief Unloaded 핸들러 인스턴스 저장(해제용) </summary>
		private static readonly DependencyProperty UnloadedHandlerProperty =
			DependencyProperty.RegisterAttached(
				"UnloadedHandler",
				typeof(RoutedEventHandler),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary> \brief Get flag </summary>
		private static bool GetIsHandlersHooked(DependencyObject o) => (bool)o.GetValue(IsHandlersHookedProperty);
		/// <summary> \brief Set flag </summary>
		private static void SetIsHandlersHooked(DependencyObject o, bool v) => o.SetValue(IsHandlersHookedProperty, v);

		/// <summary> \brief Get flag </summary>
		private static bool GetIsDropDownClosedHooked(DependencyObject o) => (bool)o.GetValue(IsDropDownClosedHookedProperty);
		/// <summary> \brief Set flag </summary>
		private static void SetIsDropDownClosedHooked(DependencyObject o, bool v) => o.SetValue(IsDropDownClosedHookedProperty, v);

		/// <summary> \brief Get flag </summary>
		private static bool GetIsUnloadedHooked(DependencyObject o) => (bool)o.GetValue(IsUnloadedHookedProperty);
		/// <summary> \brief Set flag </summary>
		private static void SetIsUnloadedHooked(DependencyObject o, bool v) => o.SetValue(IsUnloadedHookedProperty, v);

		/// <summary> \brief DropDownClosed handler 저장 </summary>
		private static void SetDropDownClosedHandler(DependencyObject o, EventHandler? h) => o.SetValue(DropDownClosedHandlerProperty, h);
		/// <summary> \brief DropDownClosed handler 조회 </summary>
		private static EventHandler? GetDropDownClosedHandler(DependencyObject o) => (EventHandler?)o.GetValue(DropDownClosedHandlerProperty);

		/// <summary> \brief Unloaded handler 저장 </summary>
		private static void SetUnloadedHandler(DependencyObject o, RoutedEventHandler? h) => o.SetValue(UnloadedHandlerProperty, h);
		/// <summary> \brief Unloaded handler 조회 </summary>
		private static RoutedEventHandler? GetUnloadedHandler(DependencyObject o) => (RoutedEventHandler?)o.GetValue(UnloadedHandlerProperty);

		#endregion

		#region Attached DPs (Public)

		/// <summary> \brief Attached Command </summary>
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.RegisterAttached(
				"Command",
				typeof(ICommand),
				typeof(DreamineComboBox),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary> \brief Attached CommandParameter </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary> \brief Attached CommandTriggerName (comma-separated) </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary> \brief Set trigger list </summary>
		public static void SetCommandTriggerName(DependencyObject obj, string value) => obj.SetValue(CommandTriggerNameProperty, value);
		/// <summary> \brief Get trigger list </summary>
		public static string GetCommandTriggerName(DependencyObject obj) => (string)obj.GetValue(CommandTriggerNameProperty);

		/// <summary> \brief Set command </summary>
		public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);
		/// <summary> \brief Get command </summary>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/// <summary> \brief Set command parameter </summary>
		public static void SetCommandParameter(DependencyObject obj, object value) => obj.SetValue(CommandParameterProperty, value);
		/// <summary> \brief Get command parameter </summary>
		public static object GetCommandParameter(DependencyObject obj) => obj.GetValue(CommandParameterProperty);

		#endregion

		#region Hook Logic

		/// <summary>
		/// \brief Command가 설정될 때 이벤트 훅을 1회만 수행한다.
		/// \details
		/// - AddHandler는 제거가 어려우므로 "중복 훅" 자체를 반드시 막아야 한다.
		/// - DropDownClosed는 ComboBox 이벤트이므로 명시적으로 핸들러를 저장하고 Unloaded에서 해제한다.
		/// </summary>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not UIElement element)
				return;

			// \brief Routed 이벤트 훅은 1회만
			if (!GetIsHandlersHooked(d))
			{
				// \brief Mouse
				element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, ev) =>
				{
					TryExecuteCommand(d, s, "PreviewMouseUp", ev);
				}), true);

				element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, ev) =>
				{
					TryExecuteCommand(d, s, "MouseDoubleClick", ev);
				}), true);

				// \brief Keyboard (Down/Up)
				element.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, ev) =>
				{
					TryExecuteCommand(d, s, "PreviewKeyDown", ev);
				}), true);

				element.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler((s, ev) =>
				{
					TryExecuteCommand(d, s, "PreviewKeyUp", ev);
				}), true);

				// \brief Touch
				element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, ev) =>
				{
					TryExecuteCommand(d, s, "TouchUp", ev);
				}), true);

				// \brief Click (경로 유지)
				element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, ev) =>
				{
					TryExecuteCommand(d, s, "Click", ev);
				}), true);

				// \brief SelectionChanged
				element.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler((s, ev) =>
				{
					TryExecuteCommand(d, s, "SelectionChanged", ev);
				}), true);

				SetIsHandlersHooked(d, true);
			}

			// \brief Unloaded 훅도 1회만 (DropDownClosed 해제용)
			if (d is FrameworkElement fe && !GetIsUnloadedHooked(d))
			{
				RoutedEventHandler unloadedHandler = (_, __) =>
				{
					// \brief DropDownClosed 구독 해제
					if (d is ComboBox combo)
					{
						var h = GetDropDownClosedHandler(d);
						if (h != null)
						{
							combo.DropDownClosed -= h;
							SetDropDownClosedHandler(d, null);
						}
					}

					SetIsDropDownClosedHooked(d, false);
				};

				fe.Unloaded += unloadedHandler;
				SetUnloadedHandler(d, unloadedHandler);
				SetIsUnloadedHooked(d, true);
			}

			// \brief DropDownClosed는 ComboBox 이벤트 -> 중복 구독 방지 + 해제 가능하게 저장
			if (d is ComboBox comboBox && !GetIsDropDownClosedHooked(d))
			{
				EventHandler handler = (s, ev) =>
				{
					TryExecuteCommand(d, s, "DropDownClosed", ev);
				};

				comboBox.DropDownClosed += handler;
				SetDropDownClosedHandler(d, handler);
				SetIsDropDownClosedHooked(d, true);
			}
		}

		#endregion

		#region Execute Logic

		/// <summary>
		/// \brief RoutedEventArgs 기반 트리거 커맨드 실행
		/// \details
		/// - PreviewKeyDown은 키 반복 입력(IsRepeat)으로 여러 번 들어올 수 있으므로 기본적으로 차단한다.
		/// - parameter =
		///    - CommandParameter가 있으면 그것(명시 오버라이드)
		///    - 없으면 DreamineComboBoxCommandContext(eventName, sender, eventArgs, combo)
		/// </summary>
		private static void TryExecuteCommand(DependencyObject d, object? sender, string eventName, RoutedEventArgs eventArgs)
		{
			// \brief 키 반복 입력 방지 (원하면 옵션화 가능)
			if (eventName.Equals("PreviewKeyDown", StringComparison.OrdinalIgnoreCase) &&
				eventArgs is KeyEventArgs ke && ke.IsRepeat)
			{
				return;
			}

			var rawTrigger = GetCommandTriggerName(d);
			if (string.IsNullOrWhiteSpace(rawTrigger))
				return;

			var triggerList = rawTrigger.Split(',')
				.Select(x => x.Trim())
				.Where(x => !string.IsNullOrWhiteSpace(x));

			if (!triggerList.Contains(eventName, StringComparer.OrdinalIgnoreCase))
				return;

			var command = GetCommand(d);
			if (command == null)
				return;

			var explicitParam = GetCommandParameter(d);
			var combo = (sender as ComboBox) ?? (d as ComboBox);

			var parameter = explicitParam ?? new DreamineComboBoxCommandContext(eventName, sender, eventArgs, combo);

			if (command.CanExecute(parameter))
				command.Execute(parameter);
		}

		/// <summary>
		/// \brief EventArgs 기반 트리거 커맨드 실행 (DropDownClosed 등)
		/// \details
		/// - parameter =
		///    - CommandParameter가 있으면 그것(명시 오버라이드)
		///    - 없으면 DreamineComboBoxCommandContext(eventName, sender, eventArgs, combo)
		/// </summary>
		private static void TryExecuteCommand(DependencyObject d, object? sender, string eventName, EventArgs eventArgs)
		{
			var rawTrigger = GetCommandTriggerName(d);
			if (string.IsNullOrWhiteSpace(rawTrigger))
				return;

			var triggerList = rawTrigger.Split(',')
				.Select(x => x.Trim())
				.Where(x => !string.IsNullOrWhiteSpace(x));

			if (!triggerList.Contains(eventName, StringComparer.OrdinalIgnoreCase))
				return;

			var command = GetCommand(d);
			if (command == null)
				return;

			var explicitParam = GetCommandParameter(d);
			var combo = (sender as ComboBox) ?? (d as ComboBox);

			var parameter = explicitParam ?? new DreamineComboBoxCommandContext(eventName, sender, eventArgs, combo);

			if (command.CanExecute(parameter))
				command.Execute(parameter);
		}

		#endregion
	}
}
