using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// \if KO
	/// <para>요청한 View 또는 ViewModel을 찾을 수 없을 때 오류 메시지를 표시하는 대체 화면입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a fallback screen that displays an error message when a requested view or view model cannot be found.</para>
	/// \endif
	/// </summary>
	public class PlaceholderView : UserControl
	{
		/// <summary>
		/// \if KO
		/// <para>지정한 오류 메시지를 중앙의 빨간색 텍스트로 표시하는 대체 화면을 만듭니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a fallback view that displays the specified error message as centered red text.</para>
		/// \endif
		/// </summary>
		/// <param name="message">
		/// \if KO
		/// <para>사용자에게 표시할 오류 메시지입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The error message to display.</para>
		/// \endif
		/// </param>
		public PlaceholderView(string message)
		{
			this.Content = new TextBlock
			{
				Text = message,
				Foreground = Brushes.Red,
				FontSize = 18,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Margin = new Thickness(20)
			};
		}
	}
}
