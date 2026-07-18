using Dreamine.MVVM.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dreamine.UI.Wpf.Controls.MessageBox;
using Dreamine.UI.Wpf.Localization;

namespace Dreamine.UI.Wpf.Controls.Navigation
{
	/// <summary>
	/// \if KO
	/// <para>탐색 모음의 디자인 타임 미리보기에 사용할 예제 버튼 모음입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides sample buttons for design-time preview of the navigation bar.</para>
	/// \endif
	/// </summary>
	public class DesignButtonDatas : ObservableCollection<ButtonData>
	{
		/// <summary>
		/// \if KO
		/// <para>미리 정의된 디자인 타임 예제 버튼으로 모음을 초기화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes the collection with predefined design-time sample buttons.</para>
		/// \endif
		/// </summary>
		public DesignButtonDatas()
		{
			Add(new ButtonData { Content = "LoginAsync" });
			Add(new ButtonData { Content = "Main" });
			Add(new ButtonData { Content = "Manual" });
			Add(new ButtonData { Content = "Setting" });
			Add(new ButtonData { Content = "Register" });
			Add(new ButtonData { Content = "About", Visibility = Visibility.Hidden });
			Add(new ButtonData { Content = "About", Visibility = Visibility.Hidden });
			Add(new ButtonData { Content = "About", Visibility = Visibility.Hidden });
			Add(new ButtonData { Content = "About", Visibility = Visibility.Hidden });
			Add(new ButtonData { Content = "Exit" });

			this[Count - 1].Margin = new Thickness(0);
		}
	}

	/// <summary>
	/// \if KO
	/// <para>자동 로그아웃, 동적 버튼, 권한 및 지역화를 지원하는 탐색 모음입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Represents a navigation bar with automatic logout, dynamic buttons, permissions, and localization.</para>
	/// \endif
	/// </summary>
	public partial class DreamineNavigationBar : UserControl
	{
		/// <summary>
		/// \if KO
		/// <para>비활성 시간 제한으로 자동 로그아웃이 수행될 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Occurs when inactivity causes an automatic logout.</para>
		/// \endif
		/// </summary>
		/// <remarks>
		/// \if KO
		/// <para><see cref="AutoLogoutTimeout"/>이 0보다 클 때만 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Raised only when <see cref="AutoLogoutTimeout"/> is greater than zero.</para>
		/// \endif
		/// </remarks>
		public static event EventHandler? AutoLogoutOccurred;

		/// <summary>
		/// \if KO
		/// <para>자동 로그아웃 비활성 제한 시간을 초 단위로 가져오거나 설정하며 0은 기능을 끕니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the inactivity timeout in seconds; zero disables automatic logout.</para>
		/// \endif
		/// </summary>
		public int AutoLogoutTimeout
		{
			get => (int)GetValue(AutoLogoutTimeoutProperty);
			set => SetValue(AutoLogoutTimeoutProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="AutoLogoutTimeout"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="AutoLogoutTimeout"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty AutoLogoutTimeoutProperty =
			DependencyProperty.Register(
				nameof(AutoLogoutTimeout),
				typeof(int),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(0, OnAutoLogoutTimeoutChanged));

		/// <summary>
		/// \if KO
		/// <para>제한 시간 변경에 따라 해당 탐색 모음의 로그아웃 타이머를 재설정하거나 중지합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Resets or stops the navigation bar's logout timer when the timeout changes.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>속성이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose property changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 값과 새 값을 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the old and new values.</para>
		/// \endif
		/// </param>
		private static void OnAutoLogoutTimeoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
			{
				nav.ResetLogoutTimerIfNeeded();
			}
		}

		/// <summary>
		/// \if KO
		/// <para>전역 입력 활동을 전달할 활성 탐색 모음 인스턴스를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores active navigation-bar instances that receive global input activity.</para>
		/// \endif
		/// </summary>
		private static readonly HashSet<DreamineNavigationBar> _instances = new();

		/// <summary>
		/// \if KO
		/// <para>전역 입력 처리기가 이미 등록되었는지 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Indicates whether the global input handler has already been registered.</para>
		/// \endif
		/// </summary>
		private static volatile bool _isInputHooked = false;

		/// <summary>
		/// \if KO
		/// <para>마우스·키보드·터치 활동을 감지하는 전역 입력 처리기를 한 번 등록합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Registers the global mouse, keyboard, and touch activity handler once.</para>
		/// \endif
		/// </summary>
		private static void EnsureGlobalInputHook()
		{
			if (_isInputHooked == false)
			{
				InputManager.Current.PreProcessInput += OnGlobalInputStatic;
				_isInputHooked = true;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>사용자 입력을 감지하면 자동 로그아웃이 활성화된 모든 인스턴스의 타이머를 재설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Resets timers for all instances with automatic logout enabled when user input is detected.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>입력 관리자 이벤트 발신자입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The input-manager event source.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>준비 중인 입력 정보를 포함하는 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Data containing the staged input information.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static void OnGlobalInputStatic(object sender, PreProcessInputEventArgs e)
		{
			if (e.StagingItem.Input is MouseEventArgs or KeyboardEventArgs or TouchEventArgs)
			{
				foreach (var nav in _instances)
				{
					if (nav.AutoLogoutTimeout > 0)
						nav.InitializeOrResetLogoutTimer();
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>비활성 시간을 측정하는 디스패처 타이머를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the dispatcher timer that measures inactivity.</para>
		/// \endif
		/// </summary>
		private DispatcherTimer? _logoutTimer;

		/// <summary>
		/// \if KO
		/// <para>현재 제한 시간이 양수면 타이머를 재시작하고 아니면 중지합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Restarts the timer when the current timeout is positive; otherwise stops it.</para>
		/// \endif
		/// </summary>
		private void ResetLogoutTimerIfNeeded()
		{
			if (AutoLogoutTimeout > 0)
				InitializeOrResetLogoutTimer();
			else
				StopLogoutTimer();
		}

		/// <summary>
		/// \if KO
		/// <para>현재 제한 시간으로 로그아웃 타이머를 만들거나 다시 시작합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates or restarts the logout timer using the current timeout.</para>
		/// \endif
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// \if KO
		/// <para><see cref="AutoLogoutTimeout"/> 값으로 유효하지 않은 <see cref="TimeSpan"/>을 만들 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <see cref="AutoLogoutTimeout"/> cannot produce a valid <see cref="TimeSpan"/>.</para>
		/// \endif
		/// </exception>
		public void InitializeOrResetLogoutTimer()
		{
			if (_logoutTimer == null)
			{
				_logoutTimer = new DispatcherTimer
				{
					Interval = TimeSpan.FromSeconds(AutoLogoutTimeout)
				};
				_logoutTimer.Tick += (s, e) =>
				{
					_logoutTimer.Stop();

					OnAutoLogout();
				};
			}

			_logoutTimer.Stop();
			_logoutTimer.Interval = TimeSpan.FromSeconds(AutoLogoutTimeout);
			_logoutTimer.Start();
		}

		/// <summary>
		/// \if KO
		/// <para>로그아웃 타이머가 있으면 중지합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stops the logout timer when it exists.</para>
		/// \endif
		/// </summary>
		private void StopLogoutTimer()
		{
			_logoutTimer?.Stop();
		}

		/// <summary>
		/// \if KO
		/// <para>소유 창을 숨기고 버튼 등급을 초기화한 뒤 자동 로그아웃 이벤트와 알림을 발생시킵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Hides owned windows, resets button grades, then raises the automatic-logout event and notification.</para>
		/// \endif
		/// </summary>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>WPF 애플리케이션 또는 <see cref="ButtonDatas"/>가 초기화되지 않았을 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the WPF application or <see cref="ButtonDatas"/> is not initialized.</para>
		/// \endif
		/// </exception>
		private void OnAutoLogout()
		{
			foreach (Window window in Application.Current.Windows)
			{
				if (window.Owner != null && window.IsVisible)
					window.Hide();
			}

			foreach (var btn in ButtonDatas)
				btn.Grade = 0;

			AutoLogoutOccurred?.Invoke(this, EventArgs.Empty);

			DreamineMessageBox.ShowAsync(
				"You have been automatically logged out due to inactivity.",
				"Auto Logout",
				MessageBoxButton.OK,
				MessageBoxImage.Information,
				autoClick: MessageBoxResult.OK,
				autoClickDelaySeconds: 5);
		}

		/// <summary>
		/// \if KO
		/// <para>세션에서 한 번만 로그인을 요구하는 모드를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether login is requested only once per session.</para>
		/// \endif
		/// </summary>
		public bool OnceLogin
		{
			get => (bool)GetValue(OnceLoginProperty);
			set => SetValue(OnceLoginProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="OnceLogin"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="OnceLogin"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty OnceLoginProperty =
			DependencyProperty.Register(
				nameof(OnceLogin),
				typeof(bool),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(false, OnOnceLoginChanged));

		/// <summary>
		/// \if KO
		/// <para>한 번 로그인 설정을 전역 탐색 도우미에 반영합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Propagates the once-login setting to the global navigation helper.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>속성이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose property changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>새 설정값을 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the new setting.</para>
		/// \endif
		/// </param>
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>새 값이 <see cref="bool"/>이 아닐 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the new value is not a <see cref="bool"/>.</para>
		/// \endif
		/// </exception>
		private static void OnOnceLoginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
				VsNavigationHelper.IsOnceLoginEnabled = (bool)e.NewValue;
		}

		/// <summary>
		/// \if KO
		/// <para>구성 요소를 초기화하고 전역 입력 추적 및 인스턴스 수명 처리를 등록합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes components and registers global input tracking and instance-lifetime cleanup.</para>
		/// \endif
		/// </summary>
		public DreamineNavigationBar()
		{
			InitializeComponent();

			if (ButtonDatas == null)
				ButtonDatas = new ObservableCollection<ButtonData>();

			EnsureGlobalInputHook();
			_instances.Add(this);
			Unloaded += (_, _) => _instances.Remove(this);

			// Window.Closed 보장: Unloaded가 발생하지 않을 때도 GC 루트 제거
			Loaded += (_, _) =>
			{
				var parentWindow = Window.GetWindow(this);
				if (parentWindow != null)
					parentWindow.Closed += (_, _) => _instances.Remove(this);
			};
		}

		/// <summary>
		/// \if KO
		/// <para>탐색 UI에 사용할 언어를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the language used by the navigation UI.</para>
		/// \endif
		/// </summary>
		public new Language Language
		{
			get => (Language)GetValue(LanguageProperty);
			set => SetValue(LanguageProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="Language"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="Language"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly new DependencyProperty LanguageProperty =
			DependencyProperty.Register(nameof(Language), typeof(Language), typeof(DreamineNavigationBar), new PropertyMetadata(Language.English));

		/// <summary>
		/// \if KO
		/// <para>탐색 모음에 표시할 버튼 데이터 모음을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the button-data collection displayed by the navigation bar.</para>
		/// \endif
		/// </summary>
		public ObservableCollection<ButtonData> ButtonDatas
		{
			get => (ObservableCollection<ButtonData>)GetValue(ButtonDatasProperty);
			set => SetValue(ButtonDatasProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="ButtonDatas"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="ButtonDatas"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ButtonDatasProperty =
			DependencyProperty.Register(
				nameof(ButtonDatas),
				typeof(ObservableCollection<ButtonData>),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(new ObservableCollection<ButtonData>(), OnButtonDatasChanged));

		/// <summary>
		/// \if KO
		/// <para>버튼 모음이 교체되면 이전 알림을 해제하고 새 알림을 구독한 뒤 레이아웃을 갱신합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Rewires collection notifications and updates layout when the button collection is replaced.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>버튼 모음이 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose button collection changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 모음과 새 모음을 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the old and new collections.</para>
		/// \endif
		/// </param>
		private static void OnButtonDatasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
			{
				if (e.OldValue is ObservableCollection<ButtonData> oldList)
					oldList.CollectionChanged -= nav.ButtonDatas_CollectionChanged;

				if (e.NewValue is ObservableCollection<ButtonData> newList)
					newList.CollectionChanged += nav.ButtonDatas_CollectionChanged;

				nav.UpdateMarginAndAlignment();
			}
		}

		/// <summary>
		/// \if KO
		/// <para>버튼 모음 변경 시 여백과 정렬을 다시 계산합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Recalculates margins and alignment when the button collection changes.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>변경된 버튼 모음입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The changed button collection.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>모음 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Collection-change data.</para>
		/// \endif
		/// </param>
		private void ButtonDatas_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			UpdateMarginAndAlignment();
		}

		/// <summary>
		/// \if KO
		/// <para>탐색 모음이 배치될 방향을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the side on which the navigation bar is arranged.</para>
		/// \endif
		/// </summary>
		public NavigationBarPosition Position
		{
			get => (NavigationBarPosition)GetValue(PositionProperty);
			set => SetValue(PositionProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para><see cref="Position"/> 종속성 속성을 식별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the <see cref="Position"/> dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty PositionProperty =
			DependencyProperty.Register(
				nameof(Position),
				typeof(NavigationBarPosition),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(NavigationBarPosition.Top, OnPositionChanged));

		/// <summary>
		/// \if KO
		/// <para>배치 방향 변경 시 버튼 여백과 정렬을 다시 계산합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Recalculates button margins and alignment when the placement changes.</para>
		/// \endif
		/// </summary>
		/// <param name="d">
		/// \if KO
		/// <para>위치가 변경된 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object whose position changed.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>이전 위치와 새 위치를 포함하는 변경 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Change data containing the old and new positions.</para>
		/// \endif
		/// </param>
		private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
				nav.UpdateMarginAndAlignment();
		}

		/// <summary>
		/// \if KO
		/// <para>현재 방향에 맞는 기본 및 마지막 버튼 여백을 계산하여 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Calculates and applies default and last-button margins for the current orientation.</para>
		/// \endif
		/// </summary>
		private void UpdateMarginAndAlignment()
		{
			Thickness defaultMargin = Position switch
			{
				NavigationBarPosition.Right => new Thickness(0, 0, 0, 4),
				NavigationBarPosition.Left => new Thickness(0, 0, 0, 4),
				_ => new Thickness(0, 0, 4, 0)
			};
			Thickness lastMargin = Position switch
			{
				NavigationBarPosition.Right => new Thickness(0, 0, 0, 4),
				NavigationBarPosition.Left => new Thickness(0, 0, 0, 4),
				_ => new Thickness(0)
			};

			ApplyLastButtonMargin(defaultMargin, lastMargin);
		}

		/// <summary>
		/// \if KO
		/// <para>모든 버튼에 기본 여백을 적용하고 마지막 표시 버튼에 별도 여백을 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies the default margin to all buttons and a distinct margin to the last visible button.</para>
		/// \endif
		/// </summary>
		/// <param name="defaultMargin">
		/// \if KO
		/// <para>일반 버튼에 적용할 여백입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The margin applied to ordinary buttons.</para>
		/// \endif
		/// </param>
		/// <param name="lastMargin">
		/// \if KO
		/// <para>마지막 표시 버튼에 적용할 여백입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The margin applied to the last visible button.</para>
		/// \endif
		/// </param>
		private void ApplyLastButtonMargin(Thickness defaultMargin, Thickness lastMargin)
		{
			if (ButtonDatas == null || ButtonDatas.Count == 0)
				return;

			var visibleIndices = ButtonDatas
				.Select((btn, idx) => new { btn, idx })
				.Where(x => x.btn.Visibility == Visibility.Visible)
				.Select(x => x.idx)
				.ToList();

			if (visibleIndices.Count == 0)
				return;

			foreach (var btn in ButtonDatas)
			{
				btn.Margin = defaultMargin;
			}
			ButtonDatas[visibleIndices.Last()].Margin = lastMargin;
		}

		/// <summary>
		/// \if KO
		/// <para>모든 표시 창에서 클릭한 버튼을 찾아 포커스 가능 규칙에 따라 선택 상태를 갱신합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Finds the clicked button across displayed windows and updates selection according to focusability rules.</para>
		/// \endif
		/// </summary>
		/// <param name="clickedButton">
		/// \if KO
		/// <para>클릭된 버튼 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The clicked button data.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para>현재 WPF 애플리케이션이 없을 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when no current WPF application exists.</para>
		/// \endif
		/// </exception>
		public static void NotifyButtonClicked(ButtonData clickedButton)
		{
			var navBars = Application.Current.Windows
				.OfType<Window>()
				.SelectMany(w => FindVisualChildren<DreamineNavigationBar>(w))
				.ToList();

			ButtonData buttonData = null!;

			foreach (var nav in navBars)
			{
				if (!nav.ButtonDatas.Contains(clickedButton))
					continue;

				foreach (var btn in nav.ButtonDatas)
				{
					if (btn.IsSelected == true && btn.IsFocusableEx == true)
					{
						buttonData = btn;
					}
					btn.IsSelected = false;
				}
				if (clickedButton.IsFocusableEx)
				{
					clickedButton.IsSelected = true;
				}
				else
				{
					foreach (var btn in nav.ButtonDatas)
					{
						if (btn == buttonData)
						{
							btn.IsSelected = true;
						}
					}
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>시각적 트리를 재귀 탐색하여 지정한 형식의 모든 자식을 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Recursively traverses the visual tree and returns all children of the specified type.</para>
		/// \endif
		/// </summary>
		/// <typeparam name="T">
		/// \if KO
		/// <para>찾을 종속성 객체 형식입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency-object type to find.</para>
		/// \endif
		/// </typeparam>
		/// <param name="depObj">
		/// \if KO
		/// <para>탐색을 시작할 객체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The object at which to begin traversal.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>일치하는 시각적 자식의 지연 시퀀스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A deferred sequence of matching visual descendants.</para>
		/// \endif
		/// </returns>
		private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null)
				yield break;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

				if (child is T t)
					yield return t;

				foreach (var childOfChild in FindVisualChildren<T>(child))
					yield return childOfChild;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>내부 탐색 버튼 클릭 이벤트를 받기 위한 확장 지점입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Provides an extension point for internal navigation-button click events.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>클릭된 버튼입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The clicked button.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>클릭 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Click-event data.</para>
		/// \endif
		/// </param>
		/// <remarks>
		/// \if KO
		/// <para>현재 구현은 의도적으로 아무 작업도 하지 않습니다.</para>
		/// \endif
		/// \if EN
		/// <para>The current implementation intentionally performs no action.</para>
		/// \endif
		/// </remarks>
		private void DreamineButton_Click(object sender, RoutedEventArgs e)
		{
		}
	}

	/// <summary>
	/// \if KO
	/// <para>탐색 모음의 배치 방향을 정의합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Defines the placement direction of the navigation bar.</para>
	/// \endif
	/// </summary>
	public enum NavigationBarPosition
	{
		/// <summary>
		/// \if KO
		/// <para>위쪽 배치입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the bar at the top.</para>
		/// \endif
		/// </summary>
		Top,
		/// <summary>
		/// \if KO
		/// <para>아래쪽 배치입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the bar at the bottom.</para>
		/// \endif
		/// </summary>
		Bottom,
		/// <summary>
		/// \if KO
		/// <para>왼쪽 배치입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the bar on the left.</para>
		/// \endif
		/// </summary>
		Left,
		/// <summary>
		/// \if KO
		/// <para>오른쪽 배치입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Places the bar on the right.</para>
		/// \endif
		/// </summary>
		Right
	}

	/// <summary>
	/// \if KO
	/// <para>동적 탐색 버튼의 콘텐츠, 스타일, 명령, 선택 및 권한 상태를 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Represents content, styling, command, selection, and permission state for a dynamic navigation button.</para>
	/// \endif
	/// </summary>
	public class ButtonData : ViewModelBase
	{
		/// <summary>
		/// \if KO
		/// <para>버튼 콘텐츠의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the button content.</para>
		/// \endif
		/// </summary>
		private string _content = string.Empty;
		/// <summary>
		/// \if KO
		/// <para>버튼에 표시할 텍스트를 가져오거나 설정하며 변경 시 이미지 원본 알림도 발생시킵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the displayed text and also raises an image-source notification when changed.</para>
		/// \endif
		/// </summary>
		public string Content { get => _content; set { if (SetProperty(ref _content, value)) OnPropertyChanged(nameof(ImageSource)); } }

		/// <summary>
		/// \if KO
		/// <para>이미지 경로의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the image path.</para>
		/// \endif
		/// </summary>
		private string _imagePath = string.Empty;
		/// <summary>
		/// \if KO
		/// <para>버튼 이미지 경로를 가져오거나 설정하며 변경 시 이미지 원본 알림을 발생시킵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the button image path and raises an image-source notification when changed.</para>
		/// \endif
		/// </summary>
		public string ImagePath
		{
			get => _imagePath;
			set { if (SetProperty(ref _imagePath, value)) OnPropertyChanged(nameof(ImageSource)); }
		}

		/// <summary>
		/// \if KO
		/// <para>현재 이미지 경로에서 만든 이미지 원본을 가져오며 로드 실패 시 <see langword="null"/>을 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets an image source created from the current path, or <see langword="null"/> when loading fails.</para>
		/// \endif
		/// </summary>
		public ImageSource ImageSource
		{
			get
			{
				if (string.IsNullOrWhiteSpace(ImagePath)) return null!;
				try { return new BitmapImage(new Uri(ImagePath, UriKind.RelativeOrAbsolute)); }
				catch (Exception ex) { Debug.WriteLine($"[ButtonData] ImageSource 로드 실패({ImagePath}): {ex.Message}"); return null!; }
			}
		}

		/// <summary>
		/// \if KO
		/// <para>전경 브러시의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the foreground brush.</para>
		/// \endif
		/// </summary>
		private Brush _foreground = Brushes.Black;
		/// <summary>
		/// \if KO
		/// <para>버튼 전경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the button foreground brush.</para>
		/// \endif
		/// </summary>
		public Brush Foreground { get => _foreground; set => SetProperty(ref _foreground, value); }

		/// <summary>
		/// \if KO
		/// <para>위쪽 광택 브러시의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the upper shine brush.</para>
		/// \endif
		/// </summary>
		private Brush _shineColor = Brushes.LightBlue;
		/// <summary>
		/// \if KO
		/// <para>위쪽 광택 색상을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the upper shine color.</para>
		/// \endif
		/// </summary>
		public Brush ShineColor { get => _shineColor; set => SetProperty(ref _shineColor, value); }

		/// <summary>
		/// \if KO
		/// <para>아래쪽 광택 브러시의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the lower shine brush.</para>
		/// \endif
		/// </summary>
		private Brush _shineColorBottom = Brushes.White;
		/// <summary>
		/// \if KO
		/// <para>아래쪽 광택 색상을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the lower shine color.</para>
		/// \endif
		/// </summary>
		public Brush ShineColorBottom { get => _shineColorBottom; set => SetProperty(ref _shineColorBottom, value); }

		/// <summary>
		/// \if KO
		/// <para>위쪽 배경 브러시의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the upper background brush.</para>
		/// \endif
		/// </summary>
		private Brush _backgroundTop = Brushes.Wheat;
		/// <summary>
		/// \if KO
		/// <para>위쪽 배경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the upper background brush.</para>
		/// \endif
		/// </summary>
		public Brush BackgroundTop { get => _backgroundTop; set => SetProperty(ref _backgroundTop, value); }

		/// <summary>
		/// \if KO
		/// <para>기본 배경 브러시의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the primary background brush.</para>
		/// \endif
		/// </summary>
		private Brush _background = Brushes.DarkBlue;
		/// <summary>
		/// \if KO
		/// <para>기본 배경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the primary background brush.</para>
		/// \endif
		/// </summary>
		public Brush Background { get => _background; set => SetProperty(ref _background, value); }

		/// <summary>
		/// \if KO
		/// <para>텍스트에 대한 이미지 위치를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the image position relative to the text.</para>
		/// \endif
		/// </summary>
		public IconPosition ImagePosition { get; set; } = IconPosition.Left;

		/// <summary>
		/// \if KO
		/// <para>활성 상태의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the enabled state.</para>
		/// \endif
		/// </summary>
		private bool _isEnabled = true;
		/// <summary>
		/// \if KO
		/// <para>버튼이 활성화되었는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the button is enabled.</para>
		/// \endif
		/// </summary>
		public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

		/// <summary>
		/// \if KO
		/// <para>버튼 텍스트의 글꼴 굵기를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the button text font weight.</para>
		/// \endif
		/// </summary>
		public FontWeight FontWeight { get; set; } = FontWeights.Normal;
		/// <summary>
		/// \if KO
		/// <para>버튼 표시 상태를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the button visibility.</para>
		/// \endif
		/// </summary>
		public Visibility Visibility { get; set; } = Visibility.Visible;
		/// <summary>
		/// \if KO
		/// <para>버튼 외부 여백을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the button margin.</para>
		/// \endif
		/// </summary>
		public Thickness Margin { get; set; } = new Thickness(0, 0, 4, 0);
		/// <summary>
		/// \if KO
		/// <para>명령에 전달할 매개변수를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the parameter passed to the command.</para>
		/// \endif
		/// </summary>
		public object CommandParameter { get; set; } = null!;
		/// <summary>
		/// \if KO
		/// <para>라우트 명령 대상을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the routed-command target.</para>
		/// \endif
		/// </summary>
		public IInputElement CommandTarget { get; set; } = null!;

		/// <summary>
		/// \if KO
		/// <para>명령의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the command.</para>
		/// \endif
		/// </summary>
		private ICommand? _command;
		/// <summary>
		/// \if KO
		/// <para>버튼이 실행할 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the command executed by the button.</para>
		/// \endif
		/// </summary>
		public ICommand? Command { get => _command; set => SetProperty(ref _command, value); }

		/// <summary>
		/// \if KO
		/// <para>선택 상태의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the selection state.</para>
		/// \endif
		/// </summary>
		private bool _isSelected = false;
		/// <summary>
		/// \if KO
		/// <para>버튼이 선택되었는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the button is selected.</para>
		/// \endif
		/// </summary>
		public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

		/// <summary>
		/// \if KO
		/// <para>확장 포커스 가능 상태의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the extended focusability state.</para>
		/// \endif
		/// </summary>
		private bool _isFocusableEx = true;
		/// <summary>
		/// \if KO
		/// <para>클릭 시 선택을 유지할 수 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether clicking may retain selection.</para>
		/// \endif
		/// </summary>
		public bool IsFocusableEx { get => _isFocusableEx; set => SetProperty(ref _isFocusableEx, value); }

		/// <summary>
		/// \if KO
		/// <para>현재 사용자 등급의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the current user grade.</para>
		/// \endif
		/// </summary>
		private int _grade = 0;
		/// <summary>
		/// \if KO
		/// <para>현재 사용자 등급을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the current user grade.</para>
		/// \endif
		/// </summary>
		public int Grade { get => _grade; set => SetProperty(ref _grade, value); }

		/// <summary>
		/// \if KO
		/// <para>최소 요구 등급의 저장 필드입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the minimum required grade.</para>
		/// \endif
		/// </summary>
		private int _minimumGrade = 0;
		/// <summary>
		/// \if KO
		/// <para>버튼 사용에 필요한 최소 등급을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the minimum grade required to use the button.</para>
		/// \endif
		/// </summary>
		public int MinimumGrade { get => _minimumGrade; set => SetProperty(ref _minimumGrade, value); }
	}

	/// <summary>
	/// \if KO
	/// <para>탐색 로그인 상태를 공유하고 공통 스타일의 버튼 데이터를 만드는 도우미입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides shared navigation login state and factory methods for commonly styled button data.</para>
	/// \endif
	/// </summary>
	public static class VsNavigationHelper
	{
		/// <summary>
		/// \if KO
		/// <para>한 번 로그인 모드가 활성화되었는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether once-login mode is enabled.</para>
		/// \endif
		/// </summary>
		public static bool IsOnceLoginEnabled { get; set; } = false;

		/// <summary>
		/// \if KO
		/// <para>현재 인증된 사용자 객체를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the currently authenticated user object.</para>
		/// \endif
		/// </summary>
		public static object? CurrentUser { get; set; } = null;

		/// <summary>
		/// \if KO
		/// <para>공통 탐색 스타일과 지정한 동작 상태가 적용된 버튼 데이터를 만듭니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates button data with common navigation styling and the specified behavioral state.</para>
		/// \endif
		/// </summary>
		/// <param name="content">
		/// \if KO
		/// <para>버튼에 표시할 텍스트입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The text displayed by the button.</para>
		/// \endif
		/// </param>
		/// <param name="command">
		/// \if KO
		/// <para>클릭 시 실행할 선택적 명령입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional command executed on click.</para>
		/// \endif
		/// </param>
		/// <param name="minGrade">
		/// \if KO
		/// <para>버튼 사용에 필요한 최소 등급입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The minimum grade required to use the button.</para>
		/// \endif
		/// </param>
		/// <param name="imagePath">
		/// \if KO
		/// <para>선택적 이미지 경로입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional image path.</para>
		/// \endif
		/// </param>
		/// <param name="isSelected">
		/// \if KO
		/// <para>초기 선택 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The initial selection state.</para>
		/// \endif
		/// </param>
		/// <param name="isFocusable">
		/// \if KO
		/// <para>클릭 후 선택 가능한지 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Indicates whether selection can follow a click.</para>
		/// \endif
		/// </param>
		/// <param name="visibility">
		/// \if KO
		/// <para>초기 표시 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The initial visibility.</para>
		/// \endif
		/// </param>
		/// <param name="imagePosition">
		/// \if KO
		/// <para>텍스트에 대한 이미지 위치입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The image position relative to text.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>구성된 새 <see cref="ButtonData"/> 인스턴스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A newly configured <see cref="ButtonData"/> instance.</para>
		/// \endif
		/// </returns>
		public static ButtonData Create(
			string content,
			ICommand? command = null,
			int minGrade = 0,
			string? imagePath = null,
			bool isSelected = false,
			bool isFocusable = true,
			Visibility visibility = Visibility.Visible,
			IconPosition imagePosition = IconPosition.Top)
		{
			return new ButtonData
			{
				Content = content,
				Command = command,
				MinimumGrade = minGrade,
				ImagePath = imagePath!,
				ImagePosition = imagePosition,
				FontWeight = FontWeights.Bold,
				Foreground = GetColor("#143a5a"),
				ShineColor = GetColor("#104E8B"),
				ShineColorBottom = Brushes.White,
				BackgroundTop = Brushes.White,
				Background = Brushes.LightGray,
				IsSelected = isSelected,
				IsFocusableEx = isFocusable,
				Visibility = visibility
			};
		}

		/// <summary>
		/// \if KO
		/// <para>WPF 색상 문자열을 단색 브러시로 변환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Converts a WPF color string to a solid-color brush.</para>
		/// \endif
		/// </summary>
		/// <param name="hex">
		/// \if KO
		/// <para>변환할 16진수 또는 명명된 색상 문자열입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The hexadecimal or named color string to convert.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>변환된 색상을 사용하는 새 브러시입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A new brush using the converted color.</para>
		/// \endif
		/// </returns>
		/// <exception cref="FormatException">
		/// \if KO
		/// <para><paramref name="hex"/>가 유효한 색상 표현이 아닐 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <paramref name="hex"/> is not a valid color representation.</para>
		/// \endif
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// \if KO
		/// <para>변환 결과가 <see cref="Color"/>가 아닐 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the conversion result is not a <see cref="Color"/>.</para>
		/// \endif
		/// </exception>
		private static SolidColorBrush GetColor(string hex)
		{
			return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
		}
	}
}
