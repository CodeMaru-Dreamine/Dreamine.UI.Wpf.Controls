using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Dreamine.UI.Wpf.Controls.MessageBox
{
	/// <summary>
	/// \if KO
	/// <para>여러 버튼 구성, 아이콘, 자동 클릭과 활성화 지연을 지원하는 사용자 지정 메시지 상자 창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom message-box window supporting multiple button layouts, icons, auto-click, and enable delays.</para>
	/// \endif
	/// </summary>
	public partial class DreamineMessageBoxWindow : Window
	{
		/// <summary>
		/// \if KO
		/// <para>사용자 또는 자동 클릭이 선택한 결과를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the result selected by the user or auto-click operation.</para>
		/// \endif
		/// </summary>
		public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

		/// <summary>
		/// \if KO
		/// <para>창이 닫힐 때 지연 작업을 취소하는 토큰 소스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Supplies cancellation for delayed operations when the window closes.</para>
		/// \endif
		/// </summary>
		private readonly CancellationTokenSource _cts = new();

		/// <summary>
		/// \if KO
		/// <para>표시 내용과 버튼 동작을 구성하여 메시지 상자 창을 만듭니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a message-box window with configured content and button behavior.</para>
		/// \endif
		/// </summary>
		/// <param name="title">
		/// \if KO
		/// <para>창 제목입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window title.</para>
		/// \endif
		/// </param>
		/// <param name="message">
		/// \if KO
		/// <para>표시할 메시지입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The message to display.</para>
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
		/// <param name="buttons">
		/// \if KO
		/// <para>표시할 버튼 구성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button layout to display.</para>
		/// \endif
		/// </param>
		/// <param name="autoClickTarget">
		/// \if KO
		/// <para>자동 선택할 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The result to select automatically.</para>
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

		/// <summary>
		/// \if KO
		/// <para>창 종료 시 진행 중인 지연 작업을 취소하고 토큰 소스를 해제합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Cancels pending delayed operations and disposes the token source when the window closes.</para>
		/// \endif
		/// </summary>
		/// <param name="e">
		/// \if KO
		/// <para>창 종료 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window-close event data.</para>
		/// \endif
		/// </param>
		protected override void OnClosed(EventArgs e)
		{
			_cts.Cancel();
			_cts.Dispose();
			base.OnClosed(e);
		}

		/// <summary>
		/// \if KO
		/// <para>모든 버튼을 일시적으로 비활성화하고 남은 시간을 표시한 뒤 다시 활성화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Temporarily disables all buttons, displays a countdown, and enables them again afterward.</para>
		/// \endif
		/// </summary>
		/// <param name="delaySeconds">
		/// \if KO
		/// <para>비활성화할 시간(초)입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The number of seconds to keep buttons disabled.</para>
		/// \endif
		/// </param>
		/// <param name="ct">
		/// \if KO
		/// <para>창 종료 시 작업을 중단할 취소 토큰입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A token that stops the operation when the window closes.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>지연 및 버튼 복원 작업을 나타내는 Task입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A task representing the delay and button restoration operation.</para>
		/// \endif
		/// </returns>
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
		/// \if KO
		/// <para>대상 버튼에 남은 시간을 표시하고 지연 후 해당 버튼 클릭을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Displays a countdown on the target button and triggers its click after the delay.</para>
		/// \endif
		/// </summary>
		/// <param name="target">
		/// \if KO
		/// <para>자동 실행할 버튼 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button result to trigger.</para>
		/// \endif
		/// </param>
		/// <param name="delaySeconds">
		/// \if KO
		/// <para>자동 클릭까지의 시간(초)입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Seconds before auto-click.</para>
		/// \endif
		/// </param>
		/// <param name="ct">
		/// \if KO
		/// <para>창 종료 시 작업을 중단할 취소 토큰입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A token that stops the operation when the window closes.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>카운트다운과 자동 클릭 작업을 나타내는 Task입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A task representing the countdown and auto-click operation.</para>
		/// \endif
		/// </returns>
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
		/// \if KO
		/// <para>메시지 상자 결과에 대응하는 실제 버튼 요소를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the actual button element corresponding to a message-box result.</para>
		/// \endif
		/// </summary>
		/// <param name="result">
		/// \if KO
		/// <para>버튼에 매핑할 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The result to map to a button.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>대응하는 버튼이며 없으면 null입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The corresponding button, or null when none exists.</para>
		/// \endif
		/// </returns>
		private Button? GetTargetButton(MessageBoxResult result) => result switch
		{
			MessageBoxResult.OK => BtnOk,
			MessageBoxResult.Cancel => BtnCancel,
			MessageBoxResult.Yes => BtnYes,
			MessageBoxResult.No => BtnNo,
			_ => null
		};

		/// <summary>
		/// \if KO
		/// <para>메시지 상자 아이콘 형식에 대응하는 이모지를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Returns an emoji corresponding to a message-box icon type.</para>
		/// \endif
		/// </summary>
		/// <param name="icon">
		/// \if KO
		/// <para>변환할 아이콘 형식입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The icon type to convert.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>대응하는 이모지 문자열입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The corresponding emoji string.</para>
		/// \endif
		/// </returns>
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
		/// \if KO
		/// <para>지정한 버튼 구성에 따라 각 버튼의 가시성과 초기 포커스를 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Configures each button's visibility and initial focus according to the specified layout.</para>
		/// \endif
		/// </summary>
		/// <param name="buttons">
		/// \if KO
		/// <para>표시할 버튼 구성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button layout to display.</para>
		/// \endif
		/// </param>
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
		/// \if KO
		/// <para>확인 버튼 클릭을 처리하여 결과를 설정하고 창을 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles the OK button click by setting the result and closing the window.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 버튼입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private void BtnOk_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.OK;
			Close();
		}

		/// <summary>
		/// \if KO
		/// <para>취소 버튼 클릭을 처리하여 결과를 설정하고 창을 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles the Cancel button click by setting the result and closing the window.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 버튼입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private void BtnCancel_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Cancel;
			Close();
		}

		/// <summary>
		/// \if KO
		/// <para>예 버튼 클릭을 처리하여 결과를 설정하고 창을 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles the Yes button click by setting the result and closing the window.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 버튼입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private void BtnYes_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Yes;
			Close();
		}

		/// <summary>
		/// \if KO
		/// <para>아니요 버튼 클릭을 처리하여 결과를 설정하고 창을 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles the No button click by setting the result and closing the window.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 버튼입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The button that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private void BtnNo_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.No;
			Close();
		}
	}
}
