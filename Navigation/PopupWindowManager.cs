using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static Dreamine.UI.Wpf.Controls.ViewRegion.ViewLoader;

namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// \if KO
	/// <para>View와 ViewModel을 별도 테두리 없는 최상위 <see cref="Window"/>로 감싸 등록·표시·숨김 처리합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Wraps views and view models in separate borderless topmost <see cref="Window"/> instances and manages their registration and visibility.</para>
	/// \endif
	/// </summary>
	public class PopupWindowManager
	{
		/// <summary>
		/// \if KO
		/// <para>새 팝업의 기본 너비를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the default width for new popups.</para>
		/// \endif
		/// </summary>
		public double Width { get; set; } = 800;

		/// <summary>
		/// \if KO
		/// <para>새 팝업의 기본 높이를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the default height for new popups.</para>
		/// \endif
		/// </summary>
		public double Height { get; set; } = 600;

		/// <summary>
		/// \if KO
		/// <para>공유 팝업 창 관리자 인스턴스를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the shared popup-window manager instance.</para>
		/// \endif
		/// </summary>
		public static PopupWindowManager Instance { get; } = new();
		/// <summary>
		/// \if KO
		/// <para>이름별 등록된 팝업 창 저장소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores registered popup windows by name.</para>
		/// \endif
		/// </summary>
		private readonly Dictionary<string, Window> _popupWindows = new();

		/// <summary>
		/// \if KO
		/// <para>등록된 팝업 창의 너비와 높이를 변경합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Changes the width and height of a registered popup window.</para>
		/// \endif
		/// </summary>
		/// <param name="name">
		/// \if KO
		/// <para>팝업 등록 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup registration name.</para>
		/// \endif
		/// </param>
		/// <param name="width">
		/// \if KO
		/// <para>새 너비입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The new width.</para>
		/// \endif
		/// </param>
		/// <param name="height">
		/// \if KO
		/// <para>새 높이입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The new height.</para>
		/// \endif
		/// </param>
		/// <exception cref="KeyNotFoundException">
		/// \if KO
		/// <para><paramref name="name"/>에 해당하는 팝업이 없으면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when no popup is registered for <paramref name="name"/>.</para>
		/// \endif
		/// </exception>
		/// <exception cref="ArgumentException">
		/// \if KO
		/// <para>너비 또는 높이가 WPF 창 크기로 유효하지 않으면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when width or height is invalid for a WPF window.</para>
		/// \endif
		/// </exception>
		public void SetPopupSize(string name, double width, double height)
		{
			_popupWindows[name].Width = width;
			_popupWindows[name].Height = height;
		}

		/// <summary>
		/// \if KO
		/// <para>현재 등록된 이름별 팝업 창을 읽기 전용 인터페이스로 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets currently registered popup windows through a read-only interface.</para>
		/// \endif
		/// </summary>
		public IReadOnlyDictionary<string, Window> Windows => _popupWindows;

		/// <summary>
		/// \if KO
		/// <para>로드된 View 정보를 독립 팝업 창으로 만들고 수명 주기와 위치 처리기를 연결한 뒤 등록합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates a standalone popup from loaded view information, attaches lifecycle and positioning handlers, and registers it.</para>
		/// \endif
		/// </summary>
		/// <param name="name">
		/// \if KO
		/// <para>팝업의 고유 등록 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup's unique registration name.</para>
		/// \endif
		/// </param>
		/// <param name="loadedViewInfo">
		/// \if KO
		/// <para>표시할 View 정보를 포함한 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Information containing the view to display.</para>
		/// \endif
		/// </param>
		/// <param name="popupWidth">
		/// \if KO
		/// <para>선택적 너비이며 생략하면 <see cref="Width"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Optional width, defaulting to <see cref="Width"/>.</para>
		/// \endif
		/// </param>
		/// <param name="popupHeight">
		/// \if KO
		/// <para>선택적 높이이며 생략하면 <see cref="Height"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Optional height, defaulting to <see cref="Height"/>.</para>
		/// \endif
		/// </param>
		/// <param name="centerOnScreen">
		/// \if KO
		/// <para>소유자 대신 화면 중앙에 배치하려면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> to center on the screen instead of the owner.</para>
		/// \endif
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>표시 가능한 View가 없거나 창 상태가 팝업 표시를 허용하지 않으면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when no displayable view exists or the window state does not permit showing the popup.</para>
		/// \endif
		/// </exception>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>현재 WPF 애플리케이션이 없으면 Dispatcher 접근 중 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown while accessing the dispatcher when there is no current WPF application.</para>
		/// \endif
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="name"/>이 null인 상태에서 등록하면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when registration is attempted with a null <paramref name="name"/>.</para>
		/// \endif
		/// </exception>
		public void CreatePopup(string name, LoadedViewInfo loadedViewInfo, double? popupWidth = null, double? popupHeight = null, bool centerOnScreen = false)
		{
			FrameworkElement view = loadedViewInfo.View
				?? loadedViewInfo.FrameworkView
				?? throw new InvalidOperationException($"Popup view '{name}' could not be created.");

			object? viewModel = view.DataContext;

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
#pragma warning disable CS1587 // Doxygen documents local functions; the C# compiler does not attach XML docs to them.
/// \cond LOCAL_FUNCTION_DOCUMENTATION
				/// <summary>
				/// \if KO
				/// <para>소유자 창의 콘텐츠 렌더링이 완료되면 팝업 소유권과 위치를 한 번 보정합니다.</para>
				/// \endif
				/// \if EN
				/// <para>Corrects popup ownership and position once the owner window finishes rendering its content.</para>
				/// \endif
				/// </summary>
				/// <param name="s">
				/// \if KO
				/// <para>콘텐츠 렌더링 이벤트를 발생시킨 소유자 창입니다.</para>
				/// \endif
				/// \if EN
				/// <para>The owner window that raised the content-rendered event.</para>
				/// \endif
				/// </param>
				/// <param name="e">
				/// \if KO
				/// <para>콘텐츠 렌더링 이벤트 데이터이며 이 처리기에서는 사용하지 않습니다.</para>
				/// \endif
				/// \if EN
				/// <para>The content-rendered event data, which this handler does not use.</para>
				/// \endif
				/// </param>
/// \endcond
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
#pragma warning restore CS1587

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

		/// <summary>
		/// \if KO
		/// <para>팝업을 화면 또는 표시 중인 소유자 창의 중앙에 배치합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Positions a popup at the center of the screen or its visible owner window.</para>
		/// \endif
		/// </summary>
		/// <param name="popup">
		/// \if KO
		/// <para>위치를 조정할 팝업입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup to position.</para>
		/// \endif
		/// </param>
		/// <param name="owner">
		/// \if KO
		/// <para>선택적 소유자 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional owner window.</para>
		/// \endif
		/// </param>
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
		/// \if KO
		/// <para>현재 표시 중인 등록 팝업을 닫고 내부 목록을 비웁니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closes currently visible registered popups and clears the internal list.</para>
		/// \endif
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
		/// \if KO
		/// <para>지정한 이름의 팝업을 표시하고 활성화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Shows and activates the popup registered with the specified name.</para>
		/// \endif
		/// </summary>
		/// <param name="name">
		/// \if KO
		/// <para>표시할 팝업 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup name to display.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="name"/>이 null이면 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="name"/> is null.</para>
		/// \endif
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>창을 표시하거나 활성화할 수 없는 상태이면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the window cannot be shown or activated in its current state.</para>
		/// \endif
		/// </exception>
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
		/// \if KO
		/// <para>지정한 이름을 제외한 표시 중인 모든 팝업 창을 숨깁니다.</para>
		/// \endif
		/// \if EN
		/// <para>Hides every visible popup window except the one with the specified name.</para>
		/// \endif
		/// </summary>
		/// <param name="name">
		/// \if KO
		/// <para>표시 상태를 유지할 팝업 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup name that remains visible.</para>
		/// \endif
		/// </param>
		public void HideAllExcept(string name)
		{
			foreach (var kv in _popupWindows)
			{
				if (kv.Key != name && kv.Value.IsVisible)
					kv.Value.Hide();
			}
		}

		/// <summary>
		/// \if KO
		/// <para>등록된 모든 팝업 창을 닫고 내부 사전에서 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closes every registered popup and removes all entries from the internal dictionary.</para>
		/// \endif
		/// </summary>
		public void CloseAll()
		{
			foreach (var popup in _popupWindows.Values)
				popup.Close();

			_popupWindows.Clear();
		}
	}
}
