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
	/// \if KO
	/// <para>여러 UI 이벤트를 MVVM 명령으로 전달하는 연결 트리거를 지원하는 콤보 상자입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Represents a combo box that supports attached triggers for forwarding multiple UI events to MVVM commands.</para>
	/// \endif
	/// </summary>
	public class DreamineComboBox : ComboBox
	{
		#region Command Context

		/// <summary>
		/// \if KO
		/// <para>명시적 명령 매개변수가 없을 때 전달되는 콤보 상자 이벤트 스냅숏입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Represents the combo-box event snapshot passed when no explicit command parameter is configured.</para>
		/// \endif
		/// </summary>
		public sealed class DreamineComboBoxCommandContext
		{
			/// <summary>
			/// \if KO
			/// <para>명령 실행을 유발한 이벤트 이름을 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the name of the event that triggered command execution.</para>
			/// \endif
			/// </summary>
			public string EventName { get; }

			/// <summary>
			/// \if KO
			/// <para>원래 이벤트 발신자를 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the original event sender.</para>
			/// \endif
			/// </summary>
			public object? Sender { get; }

			/// <summary>
			/// \if KO
			/// <para>원래 이벤트 데이터를 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the original event data.</para>
			/// \endif
			/// </summary>
			public object? EventArgs { get; }

			/// <summary>
			/// \if KO
			/// <para>이벤트와 연결된 콤보 상자 인스턴스를 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the combo-box instance associated with the event.</para>
			/// \endif
			/// </summary>
			public ComboBox? ComboBox { get; }

			/// <summary>
			/// \if KO
			/// <para>컨텍스트 생성 당시 선택 항목을 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the selected-item snapshot captured when the context was created.</para>
			/// \endif
			/// </summary>
			public object? SelectedItem { get; }

			/// <summary>
			/// \if KO
			/// <para>컨텍스트 생성 당시 선택 값을 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the selected-value snapshot captured when the context was created.</para>
			/// \endif
			/// </summary>
			public object? SelectedValue { get; }

			/// <summary>
			/// \if KO
			/// <para>컨텍스트 생성 당시 선택 인덱스를 가져옵니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets the selected-index snapshot captured when the context was created.</para>
			/// \endif
			/// </summary>
			public int SelectedIndex { get; }

			/// <summary>
			/// \if KO
			/// <para>이벤트 정보와 현재 선택 상태로 명령 컨텍스트를 만듭니다.</para>
			/// \endif
			/// \if EN
			/// <para>Initializes a command context from event information and the current selection state.</para>
			/// \endif
			/// </summary>
			/// <param name="eventName">
			/// \if KO
			/// <para>트리거 이벤트 이름입니다. <see langword="null"/>이면 빈 문자열로 저장됩니다.</para>
			/// \endif
			/// \if EN
			/// <para>The trigger-event name; <see langword="null"/> is stored as an empty string.</para>
			/// \endif
			/// </param>
			/// <param name="sender">
			/// \if KO
			/// <para>원래 이벤트 발신자입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The original event sender.</para>
			/// \endif
			/// </param>
			/// <param name="args">
			/// \if KO
			/// <para>원래 이벤트 데이터입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The original event data.</para>
			/// \endif
			/// </param>
			/// <param name="combo">
			/// \if KO
			/// <para>선택 상태를 읽을 콤보 상자입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The combo box from which to capture selection state.</para>
			/// \endif
			/// </param>
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

			/// <summary>
			/// \if KO
			/// <para>이벤트와 선택 상태를 나타내는 진단 문자열을 반환합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Returns a diagnostic string describing the event and selection state.</para>
			/// \endif
			/// </summary>
			/// <returns>
			/// \if KO
			/// <para>이벤트 이름, 선택 항목 및 선택 인덱스가 포함된 문자열입니다.</para>
			/// \endif
			/// \if EN
			/// <para>A string containing the event name, selected item, and selected index.</para>
			/// \endif
			/// </returns>
			public override string ToString()
				=> $"Event={EventName}, SelectedItem={SelectedItem ?? "(null)"}, SelectedIndex={SelectedIndex}";
		}

		#endregion

		#region Attached Flags (Private)

		/// <summary>
		/// \if KO
		/// <para>라우트 이벤트 처리기가 이미 연결되었는지 나타내는 내부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal attached flag indicating whether routed-event handlers are already connected.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsHandlersHooked",
				typeof(bool),
				typeof(DreamineComboBox),
				new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>드롭다운 닫힘 처리기의 중복 구독을 방지하는 내부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal attached flag that prevents duplicate drop-down-closed subscriptions.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsDropDownClosedHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsDropDownClosedHooked",
				typeof(bool),
				typeof(DreamineComboBox),
				new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>언로드 처리기의 중복 구독을 방지하는 내부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal attached flag that prevents duplicate unloaded-handler subscriptions.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty IsUnloadedHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsUnloadedHooked",
				typeof(bool),
				typeof(DreamineComboBox),
				new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>나중에 해제할 드롭다운 닫힘 처리기를 보관하는 내부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal attached property that stores the drop-down-closed handler for later removal.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty DropDownClosedHandlerProperty =
			DependencyProperty.RegisterAttached(
				"DropDownClosedHandler",
				typeof(EventHandler),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>언로드 처리기 인스턴스를 보관하는 내부 연결 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the internal attached property that stores the unloaded-handler instance.</para>
		/// \endif
		/// </summary>
		private static readonly DependencyProperty UnloadedHandlerProperty =
			DependencyProperty.RegisterAttached(
				"UnloadedHandler",
				typeof(RoutedEventHandler),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>라우트 이벤트 처리기 연결 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether routed-event handlers are connected.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>상태를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the state.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>처리기가 연결되었으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> if the handlers are connected.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static bool GetIsHandlersHooked(DependencyObject o) => (bool)o.GetValue(IsHandlersHookedProperty);
		/// <summary>
		/// \if KO
		/// <para>라우트 이벤트 처리기 연결 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether routed-event handlers are connected.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>상태를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the state.</para>
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
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static void SetIsHandlersHooked(DependencyObject o, bool v) => o.SetValue(IsHandlersHookedProperty, v);

		/// <summary>
		/// \if KO
		/// <para>드롭다운 닫힘 처리기 연결 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether the drop-down-closed handler is connected.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>상태를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the state.</para>
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
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static bool GetIsDropDownClosedHooked(DependencyObject o) => (bool)o.GetValue(IsDropDownClosedHookedProperty);
		/// <summary>
		/// \if KO
		/// <para>드롭다운 닫힘 처리기 연결 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether the drop-down-closed handler is connected.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>상태를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the state.</para>
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
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static void SetIsDropDownClosedHooked(DependencyObject o, bool v) => o.SetValue(IsDropDownClosedHookedProperty, v);

		/// <summary>
		/// \if KO
		/// <para>언로드 처리기 연결 여부를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets whether the unloaded handler is connected.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>상태를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the state.</para>
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
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static bool GetIsUnloadedHooked(DependencyObject o) => (bool)o.GetValue(IsUnloadedHookedProperty);
		/// <summary>
		/// \if KO
		/// <para>언로드 처리기 연결 여부를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets whether the unloaded handler is connected.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>상태를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the state.</para>
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
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static void SetIsUnloadedHooked(DependencyObject o, bool v) => o.SetValue(IsUnloadedHookedProperty, v);

		/// <summary>
		/// \if KO
		/// <para>해제에 사용할 드롭다운 닫힘 처리기를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the drop-down-closed handler used for later removal.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>처리기를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the handler.</para>
		/// \endif
		/// </param>
		/// <param name="h">
		/// \if KO
		/// <para>저장할 처리기이며 제거 시 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The handler to store, or <see langword="null"/> when clearing it.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static void SetDropDownClosedHandler(DependencyObject o, EventHandler? h) => o.SetValue(DropDownClosedHandlerProperty, h);
		/// <summary>
		/// \if KO
		/// <para>저장된 드롭다운 닫힘 처리기를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the stored drop-down-closed handler.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>처리기를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the handler.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>저장된 처리기이며 없으면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The stored handler, or <see langword="null"/> if absent.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static EventHandler? GetDropDownClosedHandler(DependencyObject o) => (EventHandler?)o.GetValue(DropDownClosedHandlerProperty);

		/// <summary>
		/// \if KO
		/// <para>언로드 처리기를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the unloaded handler.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>처리기를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the handler.</para>
		/// \endif
		/// </param>
		/// <param name="h">
		/// \if KO
		/// <para>저장할 처리기입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The handler to store.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static void SetUnloadedHandler(DependencyObject o, RoutedEventHandler? h) => o.SetValue(UnloadedHandlerProperty, h);
		/// <summary>
		/// \if KO
		/// <para>저장된 언로드 처리기를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the stored unloaded handler.</para>
		/// \endif
		/// </summary>
		/// <param name="o">
		/// \if KO
		/// <para>처리기를 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the handler.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>저장된 처리기이며 없으면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The stored handler, or <see langword="null"/> if absent.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="o"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="o"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static RoutedEventHandler? GetUnloadedHandler(DependencyObject o) => (RoutedEventHandler?)o.GetValue(UnloadedHandlerProperty);

		#endregion

		#region Attached DPs (Public)

		/// <summary>
		/// \if KO
		/// <para>이벤트와 일치할 때 실행할 연결 명령을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the attached command executed when an event matches.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.RegisterAttached(
				"Command",
				typeof(ICommand),
				typeof(DreamineComboBox),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \if KO
		/// <para>명령에 전달할 선택적 연결 매개변수를 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the optional attached parameter passed to the command.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>명령을 실행할 쉼표 구분 이벤트 이름 목록을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the comma-separated event-name list that triggers the command.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineComboBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 명령 트리거 목록을 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Sets the command-trigger list on the specified object.</para>
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
		public static void SetCommandTriggerName(DependencyObject obj, string value) => obj.SetValue(CommandTriggerNameProperty, value);
		/// <summary>
		/// \if KO
		/// <para>지정한 객체의 명령 트리거 목록을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command-trigger list from the specified object.</para>
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
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>저장된 값이 문자열이 아닐 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the stored value is not a string.</para>
		/// \endif
		/// </exception>
		public static string GetCommandTriggerName(DependencyObject obj) => (string)obj.GetValue(CommandTriggerNameProperty);

		/// <summary>
		/// \if KO
		/// <para>지정한 객체에 명령을 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attaches a command to the specified object.</para>
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
		/// <summary>
		/// \if KO
		/// <para>지정한 객체에 연결된 명령을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the command attached to the specified object.</para>
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
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>저장된 값이 <see cref="ICommand"/>가 아닐 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the stored value is not an <see cref="ICommand"/>.</para>
		/// \endif
		/// </exception>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/// <summary>
		/// \if KO
		/// <para>지정한 객체에 명령 매개변수를 설정합니다.</para>
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
		/// <para>The value to pass to the command.</para>
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
		public static void SetCommandParameter(DependencyObject obj, object value) => obj.SetValue(CommandParameterProperty, value);
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
		/// <para>명령 매개변수입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The command parameter.</para>
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
		public static object GetCommandParameter(DependencyObject obj) => obj.GetValue(CommandParameterProperty);

		#endregion

		#region Hook Logic

		/// <summary>
		/// \if KO
		/// <para>명령이 설정되면 지원 이벤트를 한 번 연결하고 언로드 시 드롭다운 처리기를 해제하도록 구성합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Hooks supported events once when a command is assigned and configures drop-down-handler cleanup on unload.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>명령 속성이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose command property changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 값과 새 값을 포함하는 속성 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Property-change data containing the old and new values.</para>
		/// \endif
		/// </param>
		/// <remarks>
		/// \if KO
		/// <para>라우트 처리기는 유지하고 직접 구독한 <c>DropDownClosed</c> 처리기만 언로드 시 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Routed handlers remain attached; only the directly subscribed <c>DropDownClosed</c> handler is removed on unload.</para>
		/// \endif
		/// </remarks>
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
		/// \if KO
		/// <para>라우트 이벤트가 구성된 트리거와 일치하면 명령을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the command when a routed event matches a configured trigger.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>연결 속성을 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the attached properties.</para>
		/// \endif
		/// </param>
		/// <param name="sender">
		/// \if KO
		/// <para>원래 이벤트 발신자입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The original event sender.</para>
		/// \endif
		/// </param>
		/// <param name="eventName">
		/// \if KO
		/// <para>비교할 이벤트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event name to compare.</para>
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
		/// <para><paramref name="d"/>, <paramref name="eventName"/> 또는 <paramref name="eventArgs"/>가 <see langword="null"/>일 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <paramref name="d"/>, <paramref name="eventName"/>, or <paramref name="eventArgs"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		/// <remarks>
		/// \if KO
		/// <para>반복된 <c>PreviewKeyDown</c>은 무시하며, 명시적 매개변수가 없으면 이벤트 스냅숏을 전달합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Repeated <c>PreviewKeyDown</c> events are ignored; an event snapshot is passed when no explicit parameter exists.</para>
		/// \endif
		/// </remarks>
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
		/// \if KO
		/// <para>일반 이벤트가 구성된 트리거와 일치하면 명령을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Executes the command when a non-routed event matches a configured trigger.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>연결 속성을 소유한 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object that owns the attached properties.</para>
		/// \endif
		/// </param>
		/// <param name="sender">
		/// \if KO
		/// <para>원래 이벤트 발신자입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The original event sender.</para>
		/// \endif
		/// </param>
		/// <param name="eventName">
		/// \if KO
		/// <para>비교할 이벤트 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The event name to compare.</para>
		/// \endif
		/// </param>
		/// <param name="eventArgs">
		/// \if KO
		/// <para>원래 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The original event data.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="d"/> 또는 <paramref name="eventName"/>가 <see langword="null"/>일 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <paramref name="d"/> or <paramref name="eventName"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		/// <remarks>
		/// \if KO
		/// <para>명시적 매개변수가 없으면 선택 상태가 포함된 이벤트 스냅숏을 전달합니다.</para>
		/// \endif
		/// \if EN
		/// <para>When no explicit parameter exists, an event snapshot containing selection state is passed.</para>
		/// \endif
		/// </remarks>
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
