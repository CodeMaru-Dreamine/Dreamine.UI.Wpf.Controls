using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static Dreamine.UI.Wpf.Controls.ViewRegion.ViewLoader;

namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// Helper class for managing popup views as separate <see cref="Window"/> instances.
	/// - Wraps a view and its corresponding ViewModel into a window.
	/// - All popups are TopMost by default and are styled without borders or taskbar presence.
	/// </summary>
	public class PopupWindowManager
	{
		/// <summary>고정 너비. null이면 미지정.</summary>
		public double Width { get; set; } = 800;

		/// <summary>고정 높이. null이면 미지정.</summary>
		public double Height { get; set; } = 600;

		public static PopupWindowManager Instance { get; set; } = new();
		private readonly Dictionary<string, Window> _popupWindows = new();

		public void SetPopupSize(string name, double whidth, double height)
		{
			Instance.Windows[name].Width = whidth;
			Instance.Windows[name].Height = height;
		}

		/// <summary>
		/// Gets the currently registered popup windows.
		/// </summary>
		public IReadOnlyDictionary<string, Window> Windows => _popupWindows;

		/// <summary>
		/// Creates and registers a new popup window with the specified view and ViewModel.
		/// </summary>
		/// <Param name="name">The unique identifier for the popup window.</Param>
		/// <Param name="view">The <see cref="FrameworkElement"/> to display in the popup.</Param>
		/// <Param name="viewModel">The ViewModel to bind to the view.</Param>
		public void CreatePopup(string name, LoadedViewInfo loadedViewInfo)
		{
			FrameworkElement view = loadedViewInfo.View == null
				? loadedViewInfo.FrameworkView
				: loadedViewInfo.View;

			object viewModel = view.DataContext;

			if (!Application.Current.Dispatcher.CheckAccess())
			{
				Application.Current.Dispatcher.Invoke(() => CreatePopup(name, loadedViewInfo));
				return;
			}

			var owner = Application.Current?.MainWindow;

			Window popup;

			view.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			var desired = view.DesiredSize;

			if (loadedViewInfo.View == null)
			{
				popup = new Window
				{
					Title = "",
					Content = view,
					DataContext = viewModel,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					WindowStyle = WindowStyle.None,
					ResizeMode = ResizeMode.NoResize,
					Background = Brushes.White,
					Width = desired.Width,
					Height = desired.Height,
					ShowInTaskbar = false,
					Topmost = true,
				};
			}
			else
			{
				popup = new Window
				{
					Title = "",
					Content = view,
					DataContext = viewModel,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
					WindowStyle = WindowStyle.None,
					ResizeMode = ResizeMode.NoResize,
					AllowsTransparency = true,
					Background = Brushes.White,
					Width = desired.Width,
					Height = desired.Height,
					ShowInTaskbar = false,
					Topmost = true
				};
			}

			// 크기 조정
			if (name.Contains("LoginAsync"))
			{
				popup.Width = 340;
				popup.Height = 400;
			}
			else if (name == "VsAlarmEdit")
			{
				popup.Width = 1024;
				popup.Height = 768;
			}
			else if (name.Contains("Alarm"))
			{
				popup.Width = 960;
				popup.Height = 600;
			}
			else
			{
				popup.Width = Width;
				popup.Height = Height;
			}

			// 최초 Owner + 위치 설정 (여기서만 Owner를 건드림)
			if (owner != null && owner.IsLoaded && owner.IsVisible)
			{
				try
				{
					popup.Owner = owner;
				}
				catch
				{
					// Owner 설정 실패 시 그냥 최상위 윈도우로 둠
				}

				if (name == "VsAlarmEdit")
				{
					var screenWidth = SystemParameters.PrimaryScreenWidth;
					var screenHeight = SystemParameters.PrimaryScreenHeight;
					popup.Left = (screenWidth - popup.Width) / 2;
					popup.Top = (screenHeight - popup.Height) / 2;
				}
				else
				{
					popup.Left = owner.Left + (owner.Width - popup.Width) / 2;
					popup.Top = owner.Top + (owner.Height - popup.Height) / 2;
				}
			}
			else
			{
				popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}

			// Owner가 나중에 보이게 되는 경우를 위한 보정
			if (owner != null && !owner.IsVisible)
			{
				void OnOwnerReady(object? s, EventArgs e)
				{
					owner.ContentRendered -= OnOwnerReady;

					if (!popup.IsVisible)
						return;

					if (!owner.IsLoaded)
						return;

					try
					{
						popup.Owner = owner;
					}
					catch { }

					if (name == "VsAlarmEdit")
					{
						var screenWidth = SystemParameters.PrimaryScreenWidth;
						var screenHeight = SystemParameters.PrimaryScreenHeight;
						popup.Left = (screenWidth - popup.Width) / 2;
						popup.Top = (screenHeight - popup.Height) / 2;
					}
					else
					{
						popup.Left = owner.Left + (owner.Width - popup.Width) / 2;
						popup.Top = owner.Top + (owner.Height - popup.Height) / 2;
					}
				}

				owner.ContentRendered += OnOwnerReady;
			}

			popup.Loaded += (s, e) => popup.Hide();
			popup.Show();
			ViewSwitcher.NotifyShown(name);

			// 위치만 조정 (Owner는 다시 세팅하지 않음)
			popup.IsVisibleChanged += (_, __) =>
			{
				if (!popup.IsVisible)
					return;

				if (name == "VsAlarmEdit")
				{
					var screenWidth = SystemParameters.PrimaryScreenWidth;
					var screenHeight = SystemParameters.PrimaryScreenHeight;
					popup.Left = (screenWidth - popup.Width) / 2;
					popup.Top = (screenHeight - popup.Height) / 2;
				}
				else if (owner != null && owner.IsLoaded && owner.IsVisible)
				{
					popup.Left = owner.Left + (owner.Width - popup.Width) / 2;
					popup.Top = owner.Top + (owner.Height - popup.Height) / 2;
				}
			};

			// 앱 종료 중이면 그냥 닫고, 평소엔 Hide 재사용
			popup.Closing += (s, e) =>
			{
				ViewSwitcher.NotifyHidden(name);

				// Owner가 이미 죽었거나, 애플리케이션이 종료 진행 중이면 그냥 닫게 둠
				var app = Application.Current;
				bool ownerDead = owner == null || !owner.IsLoaded;
				bool appShuttingDown = app is { ShutdownMode: ShutdownMode.OnExplicitShutdown } && app.MainWindow == null;

				if (ownerDead || appShuttingDown)
				{
					// e.Cancel = false → 실제로 닫힘
					return;
				}

				// 정상 동작 중에는 닫지 않고 숨김
				if (name == "VsAlarmEdit")
				{
					var screenWidth = SystemParameters.PrimaryScreenWidth;
					var screenHeight = SystemParameters.PrimaryScreenHeight;
					popup.Left = (screenWidth - popup.Width) / 2;
					popup.Top = (screenHeight - popup.Height) / 2;
				}
				else if (owner != null && owner.IsVisible)
				{
					popup.Left = owner.Left + (owner.Width - popup.Width) / 2;
					popup.Top = owner.Top + (owner.Height - popup.Height) / 2;
				}

				popup.Hide();
				e.Cancel = true;
			};

			_popupWindows[name] = popup;
		}

		/// <summary>
		/// Closes all currently registered popups and clears the internal window list.
		/// </summary>
		public void Clear()
		{
			foreach (var popup in _popupWindows.Values)
			{
				if (popup.IsVisible)
					popup.Close();
			}

			_popupWindows.Clear();
		}

		/// <summary>
		/// Shows the popup window registered with the given name.
		/// </summary>
		/// <Param name="name">The name of the popup window to display.</Param>
		public void Show(string name)
		{
			if (_popupWindows.TryGetValue(name, out var popup))
			{
				if (!popup.IsVisible)
					popup.Show();

				popup.Activate();
			}
		}

		/// <summary>
		/// Hides all popup windows except the one with the specified name.
		/// </summary>
		/// <Param name="name">The name of the popup window to remain visible.</Param>
		public void HideAllExcept(string name)
		{
			foreach (var kv in _popupWindows)
			{
				if (kv.Key != name && kv.Value.IsVisible)
					kv.Value.Hide();
			}
		}

		/// <summary>
		/// Closes all popup windows and removes them from the internal dictionary.
		/// </summary>
		public void CloseAll()
		{
			foreach (var popup in _popupWindows.Values)
				popup.Close();

			_popupWindows.Clear();
		}
	}
}
