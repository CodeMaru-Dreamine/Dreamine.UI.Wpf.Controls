using System.Windows;

namespace Dreamine.UI.Wpf.Controls.MessageBox
{
	/// <summary>
	/// Provides utility methods to display custom _message boxes with optional auto-click and delay functionality.
	/// Supports both synchronous and asynchronous execution.
	/// </summary>
	public static class DreamineMessageBox
	{
		/// <summary>
		/// Internal State tracker to prevent duplicate _message boxes from showing simultaneously.
		/// </summary>
		private static class State
		{
			private static bool _isOpen;
			private static string? _lastTitle;
			private static string? _lastMessage;
			private static MessageBoxImage _lastIcon;
			private static MessageBoxButton _lastButtons;

			/// <summary>Indicates whether a message box is currently open.</summary>
			public static bool IsOpen
			{
				get => _isOpen;
				set => _isOpen = value;
			}

			/// <summary>Stores the last shown title to avoid duplicate prompts.</summary>
			public static string? LastTitle
			{
				get => _lastTitle;
				set => _lastTitle = value;
			}

			/// <summary>Stores the last shown message.</summary>
			public static string? LastMessage
			{
				get => _lastMessage;
				set => _lastMessage = value;
			}

			/// <summary>Stores the last used icon.</summary>
			public static MessageBoxImage LastIcon
			{
				get => _lastIcon;
				set => _lastIcon = value;
			}

			/// <summary>Stores the last used button configuration.</summary>
			public static MessageBoxButton LastButtons
			{
				get => _lastButtons;
				set => _lastButtons = value;
			}

			/// <summary>
			/// Compares the provided parameters with the last _message box State.
			/// </summary>
			/// <Param name="title">The _title of the _message box.</Param>
			/// <Param name="message">The _content of the _message.</Param>
			/// <Param name="icon">The icon to display.</Param>
			/// <Param name="buttons">The buttons to display.</Param>
			/// <returns>True if the parameters match the last shown _message box and one is already open; otherwise, false.</returns>
			public static bool IsSame(string title, string message, MessageBoxImage icon, MessageBoxButton buttons) =>
				IsOpen &&
				LastTitle == title &&
				LastMessage == message &&
				LastIcon == icon &&
				LastButtons == buttons;
		}

		/// <summary>
		/// Retrieves the currently active WPF window to use as the owner of the _message box.
		/// </summary>
		/// <returns>The active <see cref="Window"/>, or null if none is active.</returns>
		private static Window? GetActiveWindow()
		{
			Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			return Application.Current.Windows
				.OfType<Window>()
				.FirstOrDefault(w => w.IsActive);
		}

		private static object _lock = new object();

		/// <summary>
		/// Displays a custom _message box in a synchronous (blocking) way.
		/// </summary>
		/// <Param name="message">The text to display in the _message box.</Param>
		/// <Param name="title">The caption of the _message box. Default is "Information".</Param>
		/// <Param name="buttons">The buttons to include in the _message box. Default is <see cref="MessageBoxButton.OK"/>.</Param>
		/// <Param name="icon">The icon to display in the _message box. Default is <see cref="MessageBoxImage.None"/>.</Param>
		/// <Param name="autoClick">The button to automatically click after delay. Default is none.</Param>
		/// <Param name="autoClickDelaySeconds">Time in seconds before <paramref name="autoClick"/> is triggered. 0 disables auto-click.</Param>
		/// <Param name="enableDelaySeconds">Time in seconds before any button becomes enabled. 0 disables delay.</Param>
		/// <returns>The result of the user's choice from the _message box.</returns>
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
		/// Displays a custom _message box asynchronously (non-blocking), with optional callback.
		/// Duplicate _message boxes with identical _content are suppressed.
		/// </summary>
		/// <Param name="message">The text to display in the _message box.</Param>
		/// <Param name="title">The caption of the _message box. Default is "Information".</Param>
		/// <Param name="buttons">The buttons to include in the _message box. Default is <see cref="MessageBoxButton.OK"/>.</Param>
		/// <Param name="icon">The icon to display in the _message box. Default is <see cref="MessageBoxImage.None"/>.</Param>
		/// <Param name="callback">Optional callback to invoke after the user closes the _message box.</Param>
		/// <Param name="autoClick">The button to automatically click after delay. Default is none.</Param>
		/// <Param name="autoClickDelaySeconds">Time in seconds before <paramref name="autoClick"/> is triggered. 0 disables auto-click.</Param>
		/// <Param name="enableDelaySeconds">Time in seconds before any button becomes enabled. 0 disables delay.</Param>
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
