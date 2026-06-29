using System.Diagnostics;
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

		public static PopupWindowManager Instance { get; } = new();
		private readonly Dictionary<string, Window> _popupWindows = new();

		public void SetPopupSize(string name, double width, double height)
		{
			_popupWindows[name].Width = width;
			_popupWindows[name].Height = height;
		}

		/// <summary>
		/// Gets the currently registered popup windows.
		/// </summary>
		public IReadOnlyDictionary<string, Window> Windows => _popupWindows;

		/// <summary>
		/// Creates and registers a new popup window with the specified view and ViewModel.
		/// </summary>
		/// <Param name="name">The unique identifier for the popup window.</Param>
		/// <Param name="loadedViewInfo">View/ViewModel info to display in the popup.</Param>
		/// <Param name="popupWidth">Optional explicit width. Uses <see cref="Width"/> default if not provided.</Param>
		/// <Param name="popupHeight">Optional explicit height. Uses <see cref="Height"/> default if not provided.</Param>
		/// <Param name="centerOnScreen">If true, centers the popup on screen rather than on the owner window.</Param>
		public void CreatePopup(string name, LoadedViewInfo loadedViewInfo, double? popupWidth = null, double? popupHeight = null, bool centerOnScreen = false)
		{
			FrameworkElement view = (loadedViewInfo.View == null
				? loadedViewInfo.FrameworkView
				: loadedViewInfo.View)!;

			object viewModel = view?.DataContext!;

			if (!Application.Current.Dispatcher.CheckAccess())
			{
				Application.Current.Dispatcher.Invoke(() => CreatePopup(name, loadedViewInfo, popupWidth, popupHeight, centerOnScreen));
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
					// AllowsTransparency=true는 Background가 완전 불투명(White)이라 시각적으로
					// 필요 없고, 레이어드 윈도우(WS_EX_LAYERED)로 만들어져 일부 환경(원격 데스크톱/
					// 가상 머신/특정 GPU 드라이버)에서 마우스 입력이 전달되지 않는 문제를 유발할 수
					// 있어 제거했다.
					ShowActivated = true,
					Background = Brushes.White,
					Width = desired.Width,
					Height = desired.Height,
					ShowInTaskbar = false,
					Topmost = true
				};
			}

			// 크기 조정
			popup.Width = popupWidth ?? Width;
			popup.Height = popupHeight ?? Height;

			// centerOnScreen 플래그를 Tag에 저장해 이후 핸들러에서 참조
			popup.Tag = centerOnScreen;

			// 최초 Owner + 위치 설정 (여기서만 Owner를 건드림)
			if (owner != null && owner.IsLoaded && owner.IsVisible)
			{
				try
				{
					popup.Owner = owner;
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[Popup] Owner 설정 실패({name}): {ex.Message}");
				}

				SetPopupPosition(popup, owner);
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
					owner!.ContentRendered -= OnOwnerReady;

					if (!popup.IsVisible)
						return;

					if (!owner.IsLoaded)
						return;

					try
					{
						popup.Owner = owner;
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"[Popup] ContentRendered Owner 설정 실패({name}): {ex.Message}");
					}

					SetPopupPosition(popup, owner);
				}

				owner.ContentRendered += OnOwnerReady;
				popup.Closed += (_, _) => owner!.ContentRendered -= OnOwnerReady;
			}

			popup.Loaded += (s, e) => popup.Hide();
			popup.Show();
			ViewSwitcher.NotifyShown(name);

			// 위치만 조정 (Owner는 다시 세팅하지 않음)
			popup.IsVisibleChanged += (_, __) =>
			{
				if (!popup.IsVisible)
					return;

				SetPopupPosition(popup, owner);
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
				SetPopupPosition(popup, owner);
				popup.Hide();
				e.Cancel = true;
			};

			_popupWindows[name] = popup;
		}

		private static void SetPopupPosition(Window popup, Window? owner)
		{
			bool centerOnScreen = popup.Tag is true;

			if (centerOnScreen)
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
