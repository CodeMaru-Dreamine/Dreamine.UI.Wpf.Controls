using System.Windows;
using System.Windows.Controls;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>기본 스타일 리소스를 자동 병합하는 Dreamine 전용 <see cref="DataGrid"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a Dreamine-specific <see cref="DataGrid"/> that automatically merges its default style resources.</para>
	/// \endif
	/// </summary>
	public class DreamineDataGrid : DataGrid
	{
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 테마 리소스 사전이 아직 없으면 애플리케이션에 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges the theme resource dictionary into the application when not already present.</para>
		/// \endif
		/// </summary>
		/// <remarks>
		/// \if KO
		/// <para>리소스 사전 로드 실패는 CLR에 의해 형식 초기화 예외로 전달될 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>A resource-dictionary load failure may be surfaced by the CLR as a type-initialization exception.</para>
		/// \endif
		/// </remarks>
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
