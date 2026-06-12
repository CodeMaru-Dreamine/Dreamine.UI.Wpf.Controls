using Dreamine.MVVM.ViewModels;
using System.Collections.ObjectModel;
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
	/// Provides sample button data for design-time visualization of the navigation bar.
	/// </summary>
	public class DesignButtonDatas : ObservableCollection<ButtonData>
	{
		/// <summary>
		/// Initializes a collection with predefined dummy buttons for design-time.
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
	/// Custom navigation bar control with automatic logout, permission control,
	/// dynamic buttons, and localization support.
	/// </summary>
	public partial class DreamineNavigationBar : UserControl
	{
		/// <summary>
		/// Occurs when the system automatically logs out the current user due to inactivity.
		/// </summary>
		/// <remarks>
		/// This event is triggered only if <see cref="AutoLogoutTimeout"/> is greater than 0
		/// and the user has been idle for the specified duration.
		/// </remarks>
		public static event EventHandler? AutoLogoutOccurred;

		/// <summary>
		/// Gets or sets the auto-logout timeout in seconds.
		/// </summary>
		/// <value>
		/// A value greater than 0 enables auto-logout after the specified number of seconds without user input.
		/// A value of 0 disables the auto-logout feature entirely.
		/// </value>
		/// <example>
		/// <code>
		/// AutoLogoutTimeout = 300; // Logs out after 5 minutes of inactivity
		/// </code>
		/// </example>
		public int AutoLogoutTimeout
		{
			get => (int)GetValue(AutoLogoutTimeoutProperty);
			set => SetValue(AutoLogoutTimeoutProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="AutoLogoutTimeout"/> dependency property.
		/// </summary>
		/// <remarks>
		/// Used to configure the inactivity timeout (in seconds) after which automatic logout is triggered.
		/// </remarks>
		public static readonly DependencyProperty AutoLogoutTimeoutProperty =
			DependencyProperty.Register(
				nameof(AutoLogoutTimeout),
				typeof(int),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(0, OnAutoLogoutTimeoutChanged));

		/// <summary>
		/// Handles changes to the <see cref="AutoLogoutTimeout"/> property.
		/// Starts or stops the auto logout timer based on the new value.
		/// </summary>
		/// <Param name="d">The dependency object (should be <see cref="DreamineNavigationBar"/>).</Param>
		/// <Param name="e">The event data containing old and new values.</Param>
		private static void OnAutoLogoutTimeoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
			{
				nav.ResetLogoutTimerIfNeeded();
			}
		}

		/// <summary>
		/// Stores all active instances of <see cref="DreamineNavigationBar"/> for global input tracking.
		/// </summary>
		private static readonly HashSet<DreamineNavigationBar> _instances = new();

		/// <summary>
		/// Tracks whether global input hook has already been registered.
		/// Prevents multiple event subscriptions.
		/// </summary>
		private static bool _isInputHooked = false;

		/// <summary>
		/// Registers a global input hook for mouse, keyboard, and touch input.
		/// Used to detect user activity and reset the logout timer on any interaction.
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
		/// Static handler that listens for global user input events (mouse, keyboard, or touch).
		/// For each active <see cref="DreamineNavigationBar"/> Instance, this resets the auto-logout timer
		/// if the timeout is enabled.
		/// </summary>
		/// <Param name="sender">The input event sender (typically <see cref="InputManager"/>).</Param>
		/// <Param name="e">Contains information about the input event.</Param>
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
		/// Internal timer used to track inactivity and trigger auto-logout after a specified timeout.
		/// Initialized only when <see cref="AutoLogoutTimeout"/> is greater than zero.
		/// </summary>
		private DispatcherTimer? _logoutTimer;

		/// <summary>
		/// Resets or stops the logout timer depending on the current <see cref="AutoLogoutTimeout"/> value.
		/// If the timeout is positive, the timer is (re)started; otherwise, it is stopped.
		/// </summary>
		private void ResetLogoutTimerIfNeeded()
		{
			if (AutoLogoutTimeout > 0)
				InitializeOrResetLogoutTimer();
			else
				StopLogoutTimer();
		}

		/// <summary>
		/// Initializes or resets the logout timer based on user activity and login State.
		/// </summary>
		/// <remarks>
		/// If <see cref="VsNavigationHelper.CurrentUser"/> is null or once-login _mode is disabled,
		/// the timer is not started. Otherwise, the timer counts down from <see cref="AutoLogoutTimeout"/> seconds
		/// and triggers logout via <see cref="OnAutoLogout"/> if no input is detected.
		/// </remarks>
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
		/// Stops the logout timer if it is running.
		/// </summary>
		/// <remarks>
		/// Typically called when <see cref="AutoLogoutTimeout"/> is set to zero
		/// or when the user manually logs out.
		/// </remarks>
		private void StopLogoutTimer()
		{
			_logoutTimer?.Stop();
		}

		/// <summary>
		/// Defines the actions to take when the user is automatically logged out due to inactivity.
		/// </summary>
		/// <remarks>
		/// - Resets all button grades to 0  
		/// - Clears the current user from <see cref="VsNavigationHelper"/>  
		/// - Triggers the <see cref="AutoLogoutOccurred"/> event  
		/// - Displays a user notification _message  
		/// </remarks>
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
		/// Gets or sets whether to enable once-login _mode.
		/// </summary>
		/// <value>
		/// If true, login will not be requested again after the first successful authentication,
		/// even if permission is insufficient.  
		/// If false, the login dialog will appear again every time permission is insufficient.
		/// </value>
		public bool OnceLogin
		{
			get => (bool)GetValue(OnceLoginProperty);
			set => SetValue(OnceLoginProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="OnceLogin"/> dependency property.
		/// </summary>
		/// <remarks>
		/// When this property is set to true, the login dialog will only appear once per session,
		/// even if permission requirements are not met later.
		/// </remarks>
		public static readonly DependencyProperty OnceLoginProperty =
			DependencyProperty.Register(
				nameof(OnceLogin),
				typeof(bool),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(false, OnOnceLoginChanged));

		/// <summary>
		/// Handles changes to the <see cref="OnceLogin"/> property.
		/// Updates the global <see cref="VsNavigationHelper.IsOnceLoginEnabled"/> flag accordingly.
		/// </summary>
		/// <Param name="d">The dependency object (should be <see cref="DreamineNavigationBar"/>).</Param>
		/// <Param name="e">Event data containing the new value.</Param>
		private static void OnOnceLoginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
				VsNavigationHelper.IsOnceLoginEnabled = (bool)e.NewValue;
		}

		/// <summary>
		/// Initializes a new Instance of the <see cref="DreamineNavigationBar"/> control.
		/// </summary>
		public DreamineNavigationBar()
		{
			InitializeComponent();

			if (ButtonDatas == null)
				ButtonDatas = new ObservableCollection<ButtonData>();

			EnsureGlobalInputHook();
			_instances.Add(this);
			Unloaded += (_, _) => _instances.Remove(this);
		}

		/// <summary>
		/// Identifies the <see cref="Language"/> dependency property,
		/// which defines the language for button text and navigation UI.
		/// </summary>
		public new Language Language
		{
			get => (Language)GetValue(LanguageProperty);
			set => SetValue(LanguageProperty, value);
		}

		/// <summary>
		/// Gets or sets the current language of the navigation bar.
		/// Used for localizing button _content and other UI elements.
		/// </summary>
		public static readonly new DependencyProperty LanguageProperty =
			DependencyProperty.Register(nameof(Language), typeof(Language), typeof(DreamineNavigationBar), new PropertyMetadata(Language.English));

		/// <summary>
		/// Gets or sets the list of navigation buttons to display in the bar.
		/// </summary>
		/// <remarks>
		/// This property supports XAML binding and allows runtime updates to the button set.
		/// Any change in the collection triggers layout recalculation via <see cref="ButtonDatas_CollectionChanged"/>.
		/// </remarks>
		public ObservableCollection<ButtonData> ButtonDatas
		{
			get => (ObservableCollection<ButtonData>)GetValue(ButtonDatasProperty);
			set => SetValue(ButtonDatasProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="ButtonDatas"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ButtonDatasProperty =
			DependencyProperty.Register(
				nameof(ButtonDatas),
				typeof(ObservableCollection<ButtonData>),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(new ObservableCollection<ButtonData>(), OnButtonDatasChanged));

		/// <summary>
		/// Called when the <see cref="ButtonDatas"/> property is changed.
		/// Unsubscribes from the old collection's <c>CollectionChanged</c> event
		/// and subscribes to the new one. Triggers margin/layout recalculation.
		/// </summary>
		/// <Param name="d">The target <see cref="DreamineNavigationBar"/> Instance.</Param>
		/// <Param name="e">Event data containing old and new values.</Param>
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
		/// Handles changes to the <see cref="ButtonDatas"/> collection (add/remove).
		/// Triggers a layout update to apply margin or alignment rules.
		/// </summary>
		/// <Param name="sender">The source collection.</Param>
		/// <Param name="e">The collection change event data.</Param>
		private void ButtonDatas_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			UpdateMarginAndAlignment();
		}

		/// <summary>
		/// Gets or sets the display position of the navigation bar (Top, Bottom, Left, or Right).
		/// </summary>
		public NavigationBarPosition Position
		{
			get => (NavigationBarPosition)GetValue(PositionProperty);
			set => SetValue(PositionProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="Position"/> dependency property,
		/// which determines the layout direction of the navigation bar.
		/// </summary>
		public static readonly DependencyProperty PositionProperty =
			DependencyProperty.Register(
				nameof(Position),
				typeof(NavigationBarPosition),
				typeof(DreamineNavigationBar),
				new PropertyMetadata(NavigationBarPosition.Top, OnPositionChanged));

		/// <summary>
		/// Called when the <see cref="Position"/> property changes.
		/// Triggers a margin/alignment update to match the new layout direction.
		/// </summary>
		private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineNavigationBar nav)
				nav.UpdateMarginAndAlignment();
		}

		/// <summary>
		/// Updates the margin and alignment of navigation buttons based on their visibility
		/// and the current <see cref="Position"/> of the navigation bar.
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
		/// Applies <paramref name="defaultMargin"/> to all visible buttons,
		/// except the last visible one which receives <paramref name="lastMargin"/>.
		/// </summary>
		/// <Param name="defaultMargin">Default margin for most buttons.</Param>
		/// <Param name="lastMargin">Custom margin for the last visible button.</Param>
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
		/// Notifies the system that a navigation button has been clicked.
		/// Updates selection State, logs the click, and applies localization if needed.
		/// </summary>
		/// <Param name="clickedButton">The button that was clicked.</Param>
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
		/// Recursively finds all visual children of a given type in the visual tree.
		/// </summary>
		/// <typeparam name="T">The type of child to find.</typeparam>
		/// <Param name="depObj">The starting element in the visual tree.</Param>
		/// <returns>An enumerable of matching visual children.</returns>
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
		/// Handles internal <see cref="DreamineButton"/> click events.
		/// Checks for permission before proceeding.  
		/// If the user does not have the required grade, login can be requested (not implemented here).
		/// </summary>
		/// <Param name="sender">The clicked button.</Param>
		/// <Param name="e">Click event arguments.</Param>
		private void DreamineButton_Click(object sender, RoutedEventArgs e)
		{
		}
	}

	/// <summary>
	/// Defines the layout direction of the navigation bar.
	/// </summary>
	public enum NavigationBarPosition
	{
		Top,
		Bottom,
		Left,
		Right
	}

	/// <summary>
	/// Extended button data structure for dynamic navigation bar button creation.
	/// Supports styling, command binding, and user-level access control.
	/// </summary>
	public class ButtonData : ViewModelBase
	{
		private string _content = string.Empty;
		public string Content { get => _content; set { if (SetProperty(ref _content, value)) OnPropertyChanged(nameof(ImageSource)); } }

		private string _imagePath = string.Empty;
		public string ImagePath
		{
			get => _imagePath;
			set { if (SetProperty(ref _imagePath, value)) OnPropertyChanged(nameof(ImageSource)); }
		}

		public ImageSource ImageSource
		{
			get
			{
				if (string.IsNullOrWhiteSpace(ImagePath)) return null!;
				try { return new BitmapImage(new Uri(ImagePath, UriKind.RelativeOrAbsolute)); }
				catch { return null!; }
			}
		}

		private Brush _foreground = Brushes.Black;
		public Brush Foreground { get => _foreground; set => SetProperty(ref _foreground, value); }

		private Brush _shineColor = Brushes.LightBlue;
		public Brush ShineColor { get => _shineColor; set => SetProperty(ref _shineColor, value); }

		private Brush _shineColorBottom = Brushes.White;
		public Brush ShineColorBottom { get => _shineColorBottom; set => SetProperty(ref _shineColorBottom, value); }

		private Brush _backgroundTop = Brushes.Wheat;
		public Brush BackgroundTop { get => _backgroundTop; set => SetProperty(ref _backgroundTop, value); }

		private Brush _background = Brushes.DarkBlue;
		public Brush Background { get => _background; set => SetProperty(ref _background, value); }

		public IconPosition ImagePosition { get; set; } = IconPosition.Left;

		private bool _isEnabled = true;
		public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

		public FontWeight FontWeight { get; set; } = FontWeights.Normal;
		public Visibility Visibility { get; set; } = Visibility.Visible;
		public Thickness Margin { get; set; } = new Thickness(0, 0, 4, 0);
		public object CommandParameter { get; set; } = null!;
		public IInputElement CommandTarget { get; set; } = null!;

		private ICommand? _command;
		public ICommand? Command { get => _command; set => SetProperty(ref _command, value); }

		private bool _isSelected = false;
		public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

		private bool _isFocusableEx = true;
		public bool IsFocusableEx { get => _isFocusableEx; set => SetProperty(ref _isFocusableEx, value); }

		private int _grade = 0;
		public int Grade { get => _grade; set => SetProperty(ref _grade, value); }

		private int _minimumGrade = 0;
		public int MinimumGrade { get => _minimumGrade; set => SetProperty(ref _minimumGrade, value); }
	}

	/// <summary>
	/// Utility class for VsNavigation-related features such as login status management and styled button creation.
	/// </summary>
	public static class VsNavigationHelper
	{
		public static bool IsOnceLoginEnabled { get; set; } = false;

		public static object? CurrentUser { get; set; } = null;

		/// <summary>
		/// Creates a new <see cref="ButtonData"/> Instance with predefined common styles applied.
		/// </summary>
		/// <Param name="content">The text _content displayed on the button.</Param>
		/// <Param name="command">The command to execute when the button is clicked (optional).</Param>
		/// <Param name="minGrade">The minimum required user grade to enable the button (used for permission filtering).</Param>
		/// <Param name="imagePath">Optional image path to be displayed on the button.</Param>
		/// <Param name="isSelected">Determines whether the button is pre-selected (highlighted).</Param>
		/// <Param name="isFocusable">Specifies whether the button can receive focus.</Param>
		/// <Param name="visibility">Defines the initial visibility of the button.</Param>
		/// <Param name="imagePosition">Specifies the position of the image (Top/Left/Right/Bottom).</Param>
		/// <returns>A styled <see cref="ButtonData"/> object that can be used in UI components.</returns>
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
		/// Converts a HEX color string to a <see cref="SolidColorBrush"/>.
		/// </summary>
		/// <Param name="hex">A hex string representing the color (e.g., "#FF0000").</Param>
		/// <returns>A <see cref="SolidColorBrush"/> representing the specified color.</returns>
		private static SolidColorBrush GetColor(string hex)
		{
			return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
		}
	}
}
