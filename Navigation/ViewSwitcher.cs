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
	/// @brief Helper responsible for switching between singleton views and popups.
	/// @details
	/// - Operates only in singleton _mode (<c>_useSingleton == true</c>).
	/// - Can activate a popup window or toggle visibility among registered singleton views.
	/// - <see cref="IsPopupName(string)"/> allows O(1) check if a name is registered as a popup.
	/// </details>
	public class ViewSwitcher
	{
		private readonly IReadOnlyDictionary<string, Window> _popupWindows;
		private readonly Dictionary<string, FrameworkElement> _views;
		private readonly bool _useSingleton;

		/// <summary>
		/// @brief Initializes a new Instance of <see cref="ViewSwitcher"/>.
		/// </summary>
		/// <Param name="popupWindows">Popup window dictionary (key=name, value=Window).</Param>
		/// <Param name="views">Standard view dictionary (key=name, value=UserControl).</Param>
		/// <Param name="useSingletonView">Whether singleton _mode is enabled.</Param>
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
		/// @brief Returns whether the given name is registered as a popup.
		/// </summary>
		/// <Param name="viewName">Name to check.</Param>
		/// <returns><c>true</c> if the key exists in popup windows; otherwise <c>false</c>.</returns>
		public bool IsPopupName(string viewName)
		{
			if (string.IsNullOrWhiteSpace(viewName)) return false;
			return _popupWindows.ContainsKey(viewName);
		}

		private static string _popupName = null!;

		public static string CurrentPopupName => _popupName;

		/// <summary>
		/// @brief Switches to a view or popup by its registered name.
		/// @details
		/// - No-op if not in singleton _mode.
		/// - If <paramref name="viewName"/> is a popup, shows and activates it.
		/// - Otherwise, optionally hides all popups (depending on <paramref name="popupChange"/>) and toggles view visibility.
		/// </details>
		/// <Param name="viewName">Target name (view or popup).</Param>
		/// <Param name="popupChange">
		/// When <c>false</c>, hide all visible popups before showing the view.
		/// When <c>true</c>, keep existing popups as-is (do not force-hide).
		/// </Param>
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

		private static readonly Dictionary<string, object> _viewModelRegistry = new();

		public static void RegisterViewModel(string viewName, object vm)
			=> _viewModelRegistry[viewName] = vm;

		public static void NotifyShown(string viewName)
		{
			if (!_viewModelRegistry.TryGetValue(viewName, out var vm)) return;
			if (vm is IActivatable act) act.Activate();
			if (vm is IVisibilityAware vis) vis.OnShown();
		}

		public static void NotifyHidden(string viewName)
		{
			if (!_viewModelRegistry.TryGetValue(viewName, out var vm)) return;
			if (vm is IVisibilityAware vis) vis.OnHidden();
			if (vm is IActivatable act) act.Deactivate();
		}
	}
}
