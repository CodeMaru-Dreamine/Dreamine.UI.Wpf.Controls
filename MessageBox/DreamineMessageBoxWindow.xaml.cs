using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Dreamine.UI.Wpf.Controls.MessageBox
{
	/// <summary>
	/// Custom _message box window for user interaction.
	/// Supports various <see cref="MessageBoxButton"/> and <see cref="MessageBoxImage"/> configurations.
	/// </summary>
	public partial class DreamineMessageBoxWindow : Window
	{
		/// <summary>
		/// Gets the result selected by the user.
		/// </summary>
		public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

		private readonly CancellationTokenSource _cts = new();

		/// <summary>
		/// Initializes a new Instance of the <see cref="DreamineMessageBoxWindow"/> class.
		/// </summary>
		/// <Param name="title">The _title of the _message box window.</Param>
		/// <Param name="message">The _message to display.</Param>
		/// <Param name="icon">The icon to display (Error, Warning, Info, Question, etc.).</Param>
		/// <Param name="buttons">The type of buttons to show (OK, OKCancel, YesNo, etc.).</Param>
		/// <Param name="autoClickTarget">If set, automatically clicks the specified result after the given delay.</Param>
		/// <Param name="autoClickDelaySeconds">Delay (in seconds) before auto-click occurs.</Param>
		/// <Param name="enableDelaySeconds">If greater than 0, disables all buttons for the given seconds before re-enabling.</Param>
		public DreamineMessageBoxWindow(string title, string message, MessageBoxImage icon, MessageBoxButton buttons, MessageBoxResult autoClickTarget = MessageBoxResult.None, int autoClickDelaySeconds = 0, int enableDelaySeconds = 0)
		{
			InitializeComponent();

			TxtTitle.Text = title;
			TxtMessage.Text = message;
			TxtIcon.Text = GetEmoji(icon);

			// 버튼 구성
			SetupButtons(buttons);

			if (enableDelaySeconds > 0)
				_ = DisableButtonsAndEnableLaterAsync(enableDelaySeconds, _cts.Token);

			if (autoClickTarget != MessageBoxResult.None && autoClickDelaySeconds > 0)
				_ = StartAutoClickAsync(autoClickTarget, autoClickDelaySeconds, _cts.Token);
		}

		protected override void OnClosed(EventArgs e)
		{
			_cts.Cancel();
			_cts.Dispose();
			base.OnClosed(e);
		}

		/// <summary>
		/// Disables all buttons temporarily and enables them again after the specified delay.
		/// Also updates the button text with countdown seconds.
		/// </summary>
		/// <Param name="delaySeconds">Time in seconds to disable the buttons.</Param>
		/// <Param name="ct">Cancellation token used when the window closes.</Param>
		private async Task DisableButtonsAndEnableLaterAsync(int delaySeconds, CancellationToken ct = default)
		{
			var buttons = new[] { BtnOk, BtnYes, BtnNo, BtnCancel };

			// 버튼별 원래 텍스트 저장
			var originalTexts = buttons.ToDictionary(btn => btn, btn => btn.Content?.ToString());

			foreach (var btn in buttons)
				btn.IsEnabled = false;

			for (int i = delaySeconds; i > 0; i--)
			{
				if (ct.IsCancellationRequested) return;

				foreach (var btn in buttons.Where(b => b.Visibility == Visibility.Visible))
					btn.Content = $"{originalTexts[btn]} ({i})";

				try { await Task.Delay(1000, ct); }
				catch (OperationCanceledException) { return; }
			}

			foreach (var btn in buttons)
			{
				if (btn.Visibility == Visibility.Visible)
				{
					btn.IsEnabled = true;
					btn.Content = originalTexts[btn];
				}
			}
		}

		/// <summary>
		/// Automatically triggers the target button click after a delay, showing a countdown in the button text.
		/// </summary>
		/// <Param name="target">The button result to trigger automatically.</Param>
		/// <Param name="delaySeconds">Time in seconds before auto-click.</Param>
		/// <Param name="ct">Cancellation token used when the window closes.</Param>
		private async Task StartAutoClickAsync(MessageBoxResult target, int delaySeconds, CancellationToken ct = default)
		{
			var button = GetTargetButton(target);
			if (button == null) return;

			string originalText = button.Content?.ToString() ?? "";

			for (int i = delaySeconds; i > 0; i--)
			{
				if (ct.IsCancellationRequested) return;

				button.Content = $"{originalText} ({i})";

				try { await Task.Delay(1000, ct); }
				catch (OperationCanceledException) { return; }
			}

			if (ct.IsCancellationRequested) return;
			button.Content = originalText;

			switch (target)
			{
				case MessageBoxResult.OK: BtnOk_Click(null!, null!); break;
				case MessageBoxResult.Yes: BtnYes_Click(null!, null!); break;
				case MessageBoxResult.No: BtnNo_Click(null!, null!); break;
				case MessageBoxResult.Cancel: BtnCancel_Click(null!, null!); break;
			}
		}

		/// <summary>
		/// Gets the actual button UI element corresponding to the specified result.
		/// </summary>
		/// <Param name="result">The result to map to a button.</Param>
		/// <returns>The corresponding button control, or null if not found.</returns>
		private Button? GetTargetButton(MessageBoxResult result) => result switch
		{
			MessageBoxResult.OK => BtnOk,
			MessageBoxResult.Cancel => BtnCancel,
			MessageBoxResult.Yes => BtnYes,
			MessageBoxResult.No => BtnNo,
			_ => null
		};

		/// <summary>
		/// Returns an emoji representation for the specified icon type.
		/// </summary>
		/// <Param name="icon">The <see cref="MessageBoxImage"/> type.</Param>
		/// <returns>An emoji string corresponding to the icon.</returns>
		private string GetEmoji(MessageBoxImage icon)
		{
			return icon switch
			{
				MessageBoxImage.Error => "❌",
				MessageBoxImage.Warning => "⚠️",
				MessageBoxImage.Information => "ℹ️",
				MessageBoxImage.Question => "❓",
				_ => "🔔"
			};
		}

		/// <summary>
		/// Configures the visibility of buttons based on the specified <see cref="MessageBoxButton"/> layout.
		/// </summary>
		/// <Param name="buttons">The button layout to display.</Param>
		private void SetupButtons(MessageBoxButton buttons)
		{
			BtnOk.Visibility = Visibility.Collapsed;
			BtnYes.Visibility = Visibility.Collapsed;
			BtnNo.Visibility = Visibility.Collapsed;
			BtnCancel.Visibility = Visibility.Collapsed;

			switch (buttons)
			{
				case MessageBoxButton.OK:
					BtnOk.Focus();
					BtnOk.Visibility = Visibility.Visible;
					break;
				case MessageBoxButton.OKCancel:
					BtnOk.Focus();
					BtnOk.Visibility = Visibility.Visible;
					BtnCancel.Visibility = Visibility.Visible;
					break;
				case MessageBoxButton.YesNo:
					BtnYes.Focus();
					BtnYes.Visibility = Visibility.Visible;
					BtnNo.Visibility = Visibility.Visible;
					break;
				case MessageBoxButton.YesNoCancel:
					BtnYes.Focus();
					BtnYes.Visibility = Visibility.Visible;
					BtnNo.Visibility = Visibility.Visible;
					BtnCancel.Visibility = Visibility.Visible;
					break;
			}
		}

		/// <summary>
		/// Handles OK button click.
		/// </summary>
		private void BtnOk_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.OK;
			Close();
		}

		/// <summary>
		/// Handles Cancel button click.
		/// </summary>
		private void BtnCancel_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Cancel;
			Close();
		}

		/// <summary>
		/// Handles Yes button click.
		/// </summary>
		private void BtnYes_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Yes;
			Close();
		}

		/// <summary>
		/// Handles No button click.
		/// </summary>
		private void BtnNo_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.No;
			Close();
		}
	}
}
