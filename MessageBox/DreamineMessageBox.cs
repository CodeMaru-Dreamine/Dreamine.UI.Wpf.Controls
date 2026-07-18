using System.Windows;

namespace Dreamine.UI.Wpf.Controls.MessageBox
{
	/// <summary>
	/// \if KO
	/// <para>자동 클릭과 버튼 활성화 지연을 지원하는 사용자 지정 메시지 상자를 동기 또는 비동기로 표시합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Displays custom message boxes synchronously or asynchronously with optional auto-click and button-enable delays.</para>
	/// \endif
	/// </summary>
	public static class DreamineMessageBox
	{
		/// <summary>
		/// \if KO
		/// <para>같은 내용의 비동기 메시지 상자가 동시에 열리지 않도록 최근 상태를 추적합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Tracks recent state to prevent identical asynchronous message boxes from being open simultaneously.</para>
		/// \endif
		/// </summary>
		private static class State
		{
			/// <summary>
			/// \if KO
			/// <para>메시지 상자가 열려 있는지 저장합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Stores whether a message box is open.</para>
			/// \endif
			/// </summary>
			private static bool _isOpen;
			/// <summary>
			/// \if KO
			/// <para>마지막 메시지 상자 제목입니다.</para>
			/// \endif
			/// \if EN
			/// <para>Stores the last message-box title.</para>
			/// \endif
			/// </summary>
			private static string? _lastTitle;
			/// <summary>
			/// \if KO
			/// <para>마지막 메시지 본문입니다.</para>
			/// \endif
			/// \if EN
			/// <para>Stores the last message text.</para>
			/// \endif
			/// </summary>
			private static string? _lastMessage;
			/// <summary>
			/// \if KO
			/// <para>마지막 아이콘입니다.</para>
			/// \endif
			/// \if EN
			/// <para>Stores the last icon.</para>
			/// \endif
			/// </summary>
			private static MessageBoxImage _lastIcon;
			/// <summary>
			/// \if KO
			/// <para>마지막 버튼 구성입니다.</para>
			/// \endif
			/// \if EN
			/// <para>Stores the last button configuration.</para>
			/// \endif
			/// </summary>
			private static MessageBoxButton _lastButtons;

			/// <summary>
			/// \if KO
			/// <para>메시지 상자가 현재 열려 있는지 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets whether a message box is currently open.</para>
			/// \endif
			/// </summary>
			public static bool IsOpen
			{
				get => _isOpen;
				set => _isOpen = value;
			}

			/// <summary>
			/// \if KO
			/// <para>마지막으로 표시한 제목을 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the last displayed title.</para>
			/// \endif
			/// </summary>
			public static string? LastTitle
			{
				get => _lastTitle;
				set => _lastTitle = value;
			}

			/// <summary>
			/// \if KO
			/// <para>마지막으로 표시한 메시지를 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the last displayed message.</para>
			/// \endif
			/// </summary>
			public static string? LastMessage
			{
				get => _lastMessage;
				set => _lastMessage = value;
			}

			/// <summary>
			/// \if KO
			/// <para>마지막으로 사용한 아이콘을 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the last used icon.</para>
			/// \endif
			/// </summary>
			public static MessageBoxImage LastIcon
			{
				get => _lastIcon;
				set => _lastIcon = value;
			}

			/// <summary>
			/// \if KO
			/// <para>마지막으로 사용한 버튼 구성을 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the last used button configuration.</para>
			/// \endif
			/// </summary>
			public static MessageBoxButton LastButtons
			{
				get => _lastButtons;
				set => _lastButtons = value;
			}

			/// <summary>
			/// \if KO
			/// <para>지정한 내용이 현재 열려 있는 마지막 메시지 상자와 같은지 확인합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Determines whether the supplied content matches the last message box that is currently open.</para>
			/// \endif
			/// </summary>
			/// <param name="title">
			/// \if KO
			/// <para>비교할 제목입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The title to compare.</para>
			/// \endif
			/// </param>
			/// <param name="message">
			/// \if KO
			/// <para>비교할 메시지입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The message to compare.</para>
			/// \endif
			/// </param>
			/// <param name="icon">
			/// \if KO
			/// <para>비교할 아이콘입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The icon to compare.</para>
			/// \endif
			/// </param>
			/// <param name="buttons">
			/// \if KO
			/// <para>비교할 버튼 구성입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The button configuration to compare.</para>
			/// \endif
			/// </param>
			/// <returns>
			/// \if KO
			/// <para>열린 메시지 상자와 모든 값이 같으면 <see langword="true"/>입니다.</para>
			/// \endif
			/// \if EN
			/// <para><see langword="true"/> when an open message box matches every value.</para>
			/// \endif
			/// </returns>
			public static bool IsSame(string title, string message, MessageBoxImage icon, MessageBoxButton buttons) =>
				IsOpen &&
				LastTitle == title &&
				LastMessage == message &&
				LastIcon == icon &&
				LastButtons == buttons;
		}

		/// <summary>
		/// \if KO
		/// <para>메시지 상자의 소유자로 사용할 현재 활성 WPF 창을 찾습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Finds the currently active WPF window to use as the message-box owner.</para>
		/// \endif
		/// </summary>
		/// <returns>
		/// \if KO
		/// <para>활성 창이며 없으면 null입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The active window, or null when none is active.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>WPF <see cref="Application.Current"/>가 없으면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when there is no current WPF <see cref="Application"/>.</para>
		/// \endif
		/// </exception>
		private static Window? GetActiveWindow()
		{
			return Application.Current.Windows
				.OfType<Window>()
				.FirstOrDefault(w => w.IsActive);
		}

		/// <summary>
		/// \if KO
		/// <para>메시지 상자 상태 동기화를 위해 예약된 잠금 개체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores a lock object reserved for message-box state synchronization.</para>
		/// \endif
		/// </summary>
		private static object _lock = new object();

		/// <summary>
		/// \if KO
		/// <para>UI Dispatcher에서 사용자 지정 메시지 상자를 모달 방식으로 표시하고 결과를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays a custom message box modally on the UI dispatcher and returns its result.</para>
		/// \endif
		/// </summary>
		/// <param name="message">
		/// \if KO
		/// <para>표시할 메시지입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The message to display.</para>
		/// \endif
		/// </param>
		/// <param name="title">
		/// \if KO
		/// <para>창 제목입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window title.</para>
		/// \endif
		/// </param>
		/// <param name="buttons">
		/// \if KO
		/// <para>표시할 버튼 구성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button configuration to display.</para>
		/// \endif
		/// </param>
		/// <param name="icon">
		/// \if KO
		/// <para>표시할 아이콘입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The icon to display.</para>
		/// \endif
		/// </param>
		/// <param name="autoClick">
		/// \if KO
		/// <para>지연 후 자동 선택할 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The result selected automatically after the delay.</para>
		/// \endif
		/// </param>
		/// <param name="autoClickDelaySeconds">
		/// \if KO
		/// <para>자동 클릭까지의 초이며 0은 비활성화입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Seconds before auto-click; zero disables it.</para>
		/// \endif
		/// </param>
		/// <param name="enableDelaySeconds">
		/// \if KO
		/// <para>버튼 활성화까지의 초이며 0은 지연 없음입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Seconds before buttons are enabled; zero means no delay.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>사용자가 선택했거나 자동 클릭된 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The user-selected or automatically selected result.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>현재 WPF 애플리케이션이 없으면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when there is no current WPF application.</para>
		/// \endif
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>Dispatcher가 종료되었거나 창을 모달로 표시할 수 없으면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the dispatcher is shut down or the window cannot be shown modally.</para>
		/// \endif
		/// </exception>
		public static MessageBoxResult Show(
			string message,
			string title = "Information",
			MessageBoxButton buttons = MessageBoxButton.OK,
			MessageBoxImage icon = MessageBoxImage.None,
			MessageBoxResult autoClick = MessageBoxResult.None, int autoClickDelaySeconds = 0, int enableDelaySeconds = 0)
		{
			return Application.Current.Dispatcher.Invoke(() =>
			{
				var msgBox = new DreamineMessageBoxWindow(title, message, icon, buttons, autoClick, autoClickDelaySeconds, enableDelaySeconds)
				{
					Owner = GetActiveWindow()
				};
				msgBox.ShowDialog();
				return msgBox.Result;
			});
		}

		/// <summary>
		/// \if KO
		/// <para>같은 내용의 중복을 억제하며 사용자 지정 메시지 상자를 Dispatcher에 비동기로 예약합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Asynchronously schedules a custom message box on the dispatcher while suppressing duplicates with identical content.</para>
		/// \endif
		/// </summary>
		/// <param name="message">
		/// \if KO
		/// <para>표시할 메시지입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The message to display.</para>
		/// \endif
		/// </param>
		/// <param name="title">
		/// \if KO
		/// <para>창 제목입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window title.</para>
		/// \endif
		/// </param>
		/// <param name="buttons">
		/// \if KO
		/// <para>표시할 버튼 구성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button configuration to display.</para>
		/// \endif
		/// </param>
		/// <param name="icon">
		/// \if KO
		/// <para>표시할 아이콘입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The icon to display.</para>
		/// \endif
		/// </param>
		/// <param name="callback">
		/// \if KO
		/// <para>창이 닫힌 뒤 결과와 함께 호출할 선택적 콜백입니다.</para>
		/// \endif
		/// \if EN
		/// <para>An optional callback invoked with the result after the window closes.</para>
		/// \endif
		/// </param>
		/// <param name="autoClick">
		/// \if KO
		/// <para>지연 후 자동 선택할 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The result selected automatically after the delay.</para>
		/// \endif
		/// </param>
		/// <param name="autoClickDelaySeconds">
		/// \if KO
		/// <para>자동 클릭까지의 초입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Seconds before auto-click.</para>
		/// \endif
		/// </param>
		/// <param name="enableDelaySeconds">
		/// \if KO
		/// <para>버튼 활성화까지의 초입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Seconds before buttons are enabled.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>현재 WPF 애플리케이션이 없으면 호출 시 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown at invocation when there is no current WPF application.</para>
		/// \endif
		/// </exception>
		/// <remarks>
		/// \if KO
		/// <para>Dispatcher 콜백 안에서 발생한 창 또는 사용자 콜백 예외는 호출자에게 동기적으로 전달되지 않습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Window or user-callback exceptions raised inside the dispatcher callback are not delivered synchronously to the caller.</para>
		/// \endif
		/// </remarks>
		public static void ShowAsync(string message, string title = "Information",
			MessageBoxButton buttons = MessageBoxButton.OK,
			MessageBoxImage icon = MessageBoxImage.None,
			Action<MessageBoxResult>? callback = null,
			MessageBoxResult autoClick = MessageBoxResult.None, int autoClickDelaySeconds = 0, int enableDelaySeconds = 0)
		{
			if (State.IsSame(title, message, icon, buttons))
				return;

			State.IsOpen = true;
			State.LastTitle = title;
			State.LastMessage = message;
			State.LastIcon = icon;
			State.LastButtons = buttons;

			Application.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					var msgBox = new DreamineMessageBoxWindow(title, message, icon, buttons, autoClick, autoClickDelaySeconds, enableDelaySeconds)
					{
						Owner = GetActiveWindow()
					};
					msgBox.ShowDialog();
					callback?.Invoke(msgBox.Result);
				}
				finally
				{
					State.IsOpen = false;
					State.LastTitle = null;
					State.LastMessage = null;
				}
			});
		}
	}
}
