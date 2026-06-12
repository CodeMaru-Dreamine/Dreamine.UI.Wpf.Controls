using System.Windows;
using System.Windows.Controls;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineDataGrid
	/// \brief Custom DataGrid control for VsLibrary with style auto-merging and MVVM-friendly extensions.
	/// 
	/// This class extends the standard <see cref="DataGrid"/> to provide:
	/// - Automatic style merging without requiring App.xaml registration.
	/// - Foundation for future MVVM features such as dynamic column binding or row-level command support.
	/// </summary>
	public class DreamineDataGrid : DataGrid
	{
		/// <summary>
		/// Static constructor for <see cref="DreamineDataGrid"/>.
		/// 
		/// This static constructor overrides the default style key and ensures that
		/// the DreamineDataGrid style is applied automatically from an embedded XAML resource.
		/// </summary>
		static DreamineDataGrid()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineDataGrid),
				new FrameworkPropertyMetadata(typeof(DreamineDataGrid)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineDataGridStyle.xaml", UriKind.RelativeOrAbsolute);

			if (Application.Current != null)
			{
				bool alreadyAdded = Application.Current.Resources.MergedDictionaries
					.OfType<ResourceDictionary>()
					.Any(x => x.Source != null && x.Source.Equals(uri));

				if (!alreadyAdded)
				{
					var dict = new ResourceDictionary { Source = uri };
					Application.Current.Resources.MergedDictionaries.Add(dict);
				}
			}
		}
	}
}