using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// A fallback view that is displayed when a corresponding View or ViewModel cannot be found.
	/// - Shows a centered error _message in red text.
	/// - Mainly used for debugging or detecting missing UI elements.
	/// </summary>
	public class PlaceholderView : UserControl
	{
		/// <summary>
		/// Initializes a new Instance of the <see cref="PlaceholderView"/> class 
		/// with the specified error _message.
		/// </summary>
		/// <Param name="message">The error _message to display to the user.</Param>
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