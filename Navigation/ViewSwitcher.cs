using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using Dreamine.MVVM.Interfaces;

namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// \if KO
	/// <para>등록된 싱글턴 View와 팝업 창을 이름으로 전환하고 ViewModel 수명 주기를 알립니다.</para>
	/// \endif
	/// \if EN
	/// <para>Switches registered singleton views and popup windows by name and notifies view-model lifecycle contracts.</para>
	/// \endif
	/// </summary>
	public class ViewSwitcher
	{
		/// <summary>
		/// \if KO
		/// <para>이름별 팝업 창 저장소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores popup windows by name.</para>
		/// \endif
		/// </summary>
		private readonly IReadOnlyDictionary<string, Window> _popupWindows;
		/// <summary>
		/// \if KO
		/// <para>이름별 일반 View 저장소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores standard views by name.</para>
		/// \endif
		/// </summary>
		private readonly Dictionary<string, FrameworkElement> _views;
		/// <summary>
		/// \if KO
		/// <para>싱글턴 View 전환을 사용할지 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Indicates whether singleton view switching is enabled.</para>
		/// \endif
		/// </summary>
		private readonly bool _useSingleton;

		/// <summary>
		/// \if KO
		/// <para>팝업과 View 저장소 및 싱글턴 모드 설정으로 전환기를 만듭니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a switcher with popup and view stores plus the singleton-mode setting.</para>
		/// \endif
		/// </summary>
		/// <param name="popupWindows">
		/// \if KO
		/// <para>이름별 팝업 창 저장소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Popup windows keyed by name.</para>
		/// \endif
		/// </param>
		/// <param name="views">
		/// \if KO
		/// <para>이름별 일반 View 저장소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Standard views keyed by name.</para>
		/// \endif
		/// </param>
		/// <param name="useSingletonView">
		/// \if KO
		/// <para>싱글턴 View 모드 사용 여부입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Whether singleton view mode is enabled.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="popupWindows"/> 또는 <paramref name="views"/>가 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="popupWindows"/> or <paramref name="views"/> is null.</para>
		/// \endif
		/// </exception>
		public ViewSwitcher(
			IReadOnlyDictionary<string, Window> popupWindows,
			Dictionary<string, FrameworkElement> views,
			bool useSingletonView)
		{
			_popupWindows = popupWindows ?? throw new ArgumentNullException(nameof(popupWindows));
			_views = views ?? throw new ArgumentNullException(nameof(views));
			_useSingleton = useSingletonView;
		}

		/// <summary>
		/// \if KO
		/// <para>지정한 이름이 팝업 창으로 등록되어 있는지 확인합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Determines whether the specified name is registered as a popup window.</para>
		/// \endif
		/// </summary>
		/// <param name="viewName">
		/// \if KO
		/// <para>확인할 View 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view name to inspect.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>팝업 이름으로 등록되어 있으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when registered as a popup name.</para>
		/// \endif
		/// </returns>
		public bool IsPopupName(string viewName)
		{
			if (string.IsNullOrWhiteSpace(viewName)) return false;
			return _popupWindows.ContainsKey(viewName);
		}

		/// <summary>
		/// \if KO
		/// <para>마지막으로 표시한 팝업 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the name of the most recently shown popup.</para>
		/// \endif
		/// </summary>
		private static string _popupName = null!;

		/// <summary>
		/// \if KO
		/// <para>마지막으로 표시한 팝업 이름을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the name of the most recently shown popup.</para>
		/// \endif
		/// </summary>
		public static string CurrentPopupName => _popupName;

		/// <summary>
		/// \if KO
		/// <para>등록 이름에 해당하는 팝업을 표시·활성화하거나 일반 View의 가시성을 전환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Shows and activates the named popup or switches visibility among standard views.</para>
		/// \endif
		/// </summary>
		/// <param name="viewName">
		/// \if KO
		/// <para>대상 View 또는 팝업 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target view or popup name.</para>
		/// \endif
		/// </param>
		/// <param name="popupChange">
		/// \if KO
		/// <para>기존 팝업을 유지하려면 <see langword="true"/>이고, 일반 View 전환 전에 숨기려면 <see langword="false"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> to preserve existing popups; <see langword="false"/> to hide them before switching to a standard view.</para>
		/// \endif
		/// </param>
		/// <remarks>
		/// \if KO
		/// <para>싱글턴 모드가 아니면 아무 작업도 수행하지 않습니다.</para>
		/// \endif
		/// \if EN
		/// <para>No action is taken when singleton mode is disabled.</para>
		/// \endif
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>표시하거나 활성화할 창의 상태가 작업을 허용하지 않으면 WPF에서 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown by WPF when a window's state does not permit showing or activation.</para>
		/// \endif
		/// </exception>
		public void Switch(string viewName, bool popupChange)
		{
			if (!_useSingleton) return;

			// Popup target
			if (IsPopupName(viewName))
			{
				var popup = _popupWindows[viewName];

				//var vm = DMContainer.Resolve(vmType);
				if (!popup.IsVisible)
				{
					NotifyShown(viewName);
					_popupName = viewName;
					popup.Show();
				}
				popup.Activate();
				return;
			}

			// View target
			if (!popupChange)
			{
				// CloseAsync popups only when caller indicates a non-popup-change flow.
				foreach (var dlg in _popupWindows.Values)
				{
					if (dlg.IsVisible)
					{
						NotifyHidden(_popupName);
						dlg.Hide();
					}
				}
			}

			foreach (var kv in _views)
			{
				kv.Value.Visibility = kv.Key == viewName
					? Visibility.Visible
					: Visibility.Hidden;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>View 이름별 ViewModel 저장소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores view models by view name.</para>
		/// \endif
		/// </summary>
		private static readonly Dictionary<string, object> _viewModelRegistry = new();

		/// <summary>
		/// \if KO
		/// <para>View 이름에 ViewModel 인스턴스를 등록하거나 교체합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Registers or replaces a view-model instance for a view name.</para>
		/// \endif
		/// </summary>
		/// <param name="viewName">
		/// \if KO
		/// <para>View 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view name.</para>
		/// \endif
		/// </param>
		/// <param name="vm">
		/// \if KO
		/// <para>연결할 ViewModel입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view model to associate.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="viewName"/>이 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="viewName"/> is null.</para>
		/// \endif
		/// </exception>
		public static void RegisterViewModel(string viewName, object vm)
			=> _viewModelRegistry[viewName] = vm;

		/// <summary>
		/// \if KO
		/// <para>등록된 ViewModel에 활성화 및 표시됨 수명 주기 알림을 전달합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Delivers activation and shown lifecycle notifications to the registered view model.</para>
		/// \endif
		/// </summary>
		/// <param name="viewName">
		/// \if KO
		/// <para>알림을 받을 View 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view name to notify.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="viewName"/>이 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="viewName"/> is null.</para>
		/// \endif
		/// </exception>
		public static void NotifyShown(string viewName)
		{
			if (!_viewModelRegistry.TryGetValue(viewName, out var vm)) return;
			if (vm is IActivatable act) act.Activate();
			if (vm is IVisibilityAware vis) vis.OnShown();
		}

		/// <summary>
		/// \if KO
		/// <para>등록된 ViewModel에 숨김 및 비활성화 수명 주기 알림을 전달합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Delivers hidden and deactivation lifecycle notifications to the registered view model.</para>
		/// \endif
		/// </summary>
		/// <param name="viewName">
		/// \if KO
		/// <para>알림을 받을 View 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view name to notify.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="viewName"/>이 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="viewName"/> is null.</para>
		/// \endif
		/// </exception>
		public static void NotifyHidden(string viewName)
		{
			if (!_viewModelRegistry.TryGetValue(viewName, out var vm)) return;
			if (vm is IVisibilityAware vis) vis.OnHidden();
			if (vm is IActivatable act) act.Deactivate();
		}
	}
}
