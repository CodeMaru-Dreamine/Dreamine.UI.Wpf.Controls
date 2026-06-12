using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class VsTimePicker
	/// \brief A custom WPF control for time input (Hour:Minute:Second) with up/down increment support.
	/// 
	/// This control allows users to input time in the format of HH:mm:ss,  
	/// and provides built-in support for incrementing or decrementing values  
	/// using buttons or keyboard input (e.g., arrow keys).
	/// </summary>
	public class DreamineTimeSpinner : Control
	{
		/// <summary>
		/// Template part name for the hour input TextBox.
		/// </summary>
		private const string PART_HourBox = "PART_HourBox";

		/// <summary>
		/// Template part name for the minute input TextBox.
		/// </summary>
		private const string PART_MinBox = "PART_MinBox";

		/// <summary>
		/// Template part name for the second input TextBox.
		/// </summary>
		private const string PART_SecBox = "PART_SecBox";

		/// <summary>
		/// Template part name for the up button (increment).
		/// </summary>
		private const string PART_UpButton = "PART_UpButton";

		/// <summary>
		/// Template part name for the down button (decrement).
		/// </summary>
		private const string PART_DownButton = "PART_DownButton";

		/// <summary>
		/// Backing field for the hour TextBox part.
		/// </summary>
		private TextBox? _hourBox;

		/// <summary>
		/// Backing field for the minute TextBox part.
		/// </summary>
		private TextBox? _minBox;

		/// <summary>
		/// Backing field for the second TextBox part.
		/// </summary>
		private TextBox? _secBox;

		/// <summary>
		/// Backing field for the up button part.
		/// </summary>
		private ButtonBase? _upButton;

		/// <summary>
		/// Backing field for the down button part.
		/// </summary>
		private ButtonBase? _downButton;

		/// <summary>
		/// Represents which time component (Hour, Minute, Second) is currently focused by the user.
		/// </summary>
		private enum eFocusedBox { Hour, Minute, Second }

		/// <summary>
		/// Tracks the currently active time input field. Defaults to <c>Minute</c>.
		/// </summary>
		private eFocusedBox _activeBox = eFocusedBox.Minute;

		/// <summary>
		/// Gets or sets the current TimeSpan value.
		/// </summary>
		public TimeSpan Time
		{
			get => (TimeSpan)GetValue(TimeProperty);
			set => SetValue(TimeProperty, value);
		}

		/// <summary>
		/// Identifies the Time dependency property.
		/// </summary>
		public static readonly DependencyProperty TimeProperty =
			DependencyProperty.Register(
				nameof(Time),
				typeof(TimeSpan),
				typeof(DreamineTimeSpinner),
				new FrameworkPropertyMetadata(default(TimeSpan), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeChanged));

		/// <summary>
		/// \brief Handles changes to the <c>Time</c> dependency property.
		///
		/// When the Time value is updated (e.g., by binding or code),  
		/// this method synchronizes the hour, minute, and second text boxes  
		/// to reflect the new TimeSpan value.
		/// </summary>
		/// <Param name="d">The dependency object where the property changed. Expected to be a DreamineTimeSpinner Instance.</Param>
		/// <Param name="e">Details about the property change event.</Param>
		private static void OnTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var spinner = d as DreamineTimeSpinner;
			spinner?.UpdateTextBoxes();
		}

		/// <summary>
		/// Up command used to increment time.
		/// </summary>
		public static RoutedCommand UpCommand { get; set; } = new(nameof(UpCommand), typeof(DreamineTimeSpinner));

		/// <summary>
		/// Down command used to decrement time.
		/// </summary>
		public static RoutedCommand DownCommand { get; set; } = new(nameof(DownCommand), typeof(DreamineTimeSpinner));

		/// <summary>
		/// Static constructor for the <see cref="DreamineTimeSpinner"/> class.
		///
		/// - Overrides the default style key to apply custom control styling.
		/// - Automatically merges <c>DreamineTimeSpinnerStyle.xaml</c> into the application resources
		///   if it is not already present, allowing the control to be styled without modifying App.xaml.
		///
		/// This ensures that the custom style is applied globally even if the user does not explicitly
		/// register the style resource.
		/// </summary>
		static DreamineTimeSpinner()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(DreamineTimeSpinner),
				new FrameworkPropertyMetadata(typeof(DreamineTimeSpinner)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineTimeSpinnerStyle.xaml", UriKind.RelativeOrAbsolute);

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

		/// <summary>
		/// Initializes a new Instance of the DreamineTimeSpinner class.
		/// </summary>
		public DreamineTimeSpinner()
		{
			CommandBindings.Add(new CommandBinding(UpCommand, OnUpExecuted));
			CommandBindings.Add(new CommandBinding(DownCommand, OnDownExecuted));
		}

		/// <summary>
		/// Called when the control template is applied. Binds parts and registers focus events.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_hourBox = GetTemplateChild(PART_HourBox) as TextBox;
			_minBox = GetTemplateChild(PART_MinBox) as TextBox;
			_secBox = GetTemplateChild(PART_SecBox) as TextBox;
			_upButton = GetTemplateChild(PART_UpButton) as ButtonBase;
			_downButton = GetTemplateChild(PART_DownButton) as ButtonBase;

			if (_hourBox != null)
			{
				_hourBox.GotFocus += (_, __) => _activeBox = eFocusedBox.Hour;
				_hourBox.LostFocus += HourBox_LostFocus;
				_hourBox.PreviewKeyDown += HourBox_PreviewKeyDown;
			}
			if (_minBox != null)
			{
				_minBox.GotFocus += (_, __) => _activeBox = eFocusedBox.Minute;
				_minBox.LostFocus += MinBox_LostFocus;
				_minBox.PreviewKeyDown += MinBox_PreviewKeyDown;
			}
			if (_secBox != null)
			{
				_secBox.GotFocus += (_, __) => _activeBox = eFocusedBox.Second;
				_secBox.LostFocus += SecBox_LostFocus;
				_secBox.PreviewKeyDown += SecBox_PreviewKeyDown;
			}

			if (_upButton != null)
				_upButton.Command = UpCommand;
			if (_downButton != null)
				_downButton.Command = DownCommand;

			UpdateTextBoxes();
		}

		/// <summary>
		/// Called when the hour box loses focus.
		/// Updates the <see cref="Time"/> property based on the current input values.
		/// </summary>
		private void HourBox_LostFocus(object sender, RoutedEventArgs e) => TryUpdateTimeFromBoxes();

		/// <summary>
		/// Called when the minute box loses focus.
		/// Updates the <see cref="Time"/> property based on the current input values.
		/// </summary>
		private void MinBox_LostFocus(object sender, RoutedEventArgs e) => TryUpdateTimeFromBoxes();

		/// <summary>
		/// Called when the second box loses focus.
		/// Updates the <see cref="Time"/> property based on the current input values.
		/// </summary>
		private void SecBox_LostFocus(object sender, RoutedEventArgs e) => TryUpdateTimeFromBoxes();

		/// <summary>
		/// Called when the Enter key is pressed in the hour box.
		/// Applies the current text to the <see cref="Time"/> value.
		/// </summary>
		private void HourBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TryUpdateTimeFromBoxes();
		}

		/// <summary>
		/// Called when the Enter key is pressed in the minute box.
		/// Applies the current text to the <see cref="Time"/> value.
		/// </summary>
		private void MinBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TryUpdateTimeFromBoxes();
		}

		/// <summary>
		/// Called when the Enter key is pressed in the second box.
		/// Applies the current text to the <see cref="Time"/> value.
		/// </summary>
		private void SecBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TryUpdateTimeFromBoxes();
		}

		/// <summary>
		/// Parses the current values from the hour, minute, and second text boxes,
		/// clamps them to valid ranges (00–23 for hours, 00–59 for minutes/seconds),
		/// and updates the <see cref="Time"/> property accordingly.
		///
		/// If parsing fails, the value defaults to 0. After update,
		/// the text boxes are refreshed to reflect the normalized time.
		/// </summary>
		private void TryUpdateTimeFromBoxes()
		{
			if (_hourBox == null || _minBox == null || _secBox == null) return;

			int h = Clamp(Parse(_hourBox.Text), 0, 23);
			int m = Clamp(Parse(_minBox.Text), 0, 59);
			int s = Clamp(Parse(_secBox.Text), 0, 59);

			Time = new TimeSpan(h, m, s);

			UpdateTextBoxes();
		}

		/// <summary>
		/// Attempts to parse a string into an integer.
		/// Returns 0 if the string is null, empty, or not a valid number.
		/// </summary>
		/// <Param name="s">The string to parse.</Param>
		/// <returns>The parsed integer, or 0 if invalid.</returns>
		private int Parse(string? s)
		{
			return int.TryParse(s, out var v) ? v : 0;
		}

		/// <summary>
		/// Ensures a given integer value falls within a specified inclusive range.
		/// </summary>
		/// <Param name="value">The value to clamp.</Param>
		/// <Param name="min">Minimum allowed value.</Param>
		/// <Param name="max">Maximum allowed value.</Param>
		/// <returns>The clamped value between min and max.</returns>
		private int Clamp(int value, int min, int max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		/// <summary>
		/// Increments the currently focused time component (hour, minute, or second).
		///
		/// This method updates the <see cref="Time"/> property by increasing the selected time segment
		/// depending on which TextBox currently has focus:
		/// - Hour: wraps from 23 → 0
		/// - Minute: 59 → 0 and increments hour (wraps hour from 23 → 0 if needed)
		/// - Second: 59 → 0 and increments minute (which may in turn increment hour)
		///
		/// Ensures time remains within the 24-hour format (00:00:00 to 23:59:59).
		/// </summary>
		/// <Param name="sender">The source object that triggered the command.</Param>
		/// <Param name="e">The event arguments containing command context.</Param>
		private void OnUpExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			switch (_activeBox)
			{
				case eFocusedBox.Hour:
					Time = new TimeSpan((Time.Hours + 1) % 24, Time.Minutes, Time.Seconds);
					break;
				case eFocusedBox.Minute:
					if (Time.Minutes == 59)
					{
						int newHour = (Time.Hours + 1) % 24;
						Time = new TimeSpan(newHour, 0, Time.Seconds);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes + 1, Time.Seconds);
					}
					break;
				case eFocusedBox.Second:
					if (Time.Seconds == 59)
					{
						int newMinute = Time.Minutes + 1;
						int newHour = Time.Hours;
						if (newMinute == 60)
						{
							newMinute = 0;
							newHour = (newHour + 1) % 24;
						}
						Time = new TimeSpan(newHour, newMinute, 0);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes, Time.Seconds + 1);
					}
					break;
			}
		}

		/// <summary>
		/// Decrements the currently focused time component (hour, minute, or second).
		/// 
		/// This method adjusts the <see cref="Time"/> property by decreasing one unit from the currently
		/// focused part (hour, minute, or second). It also handles underflow:
		/// - For hours: 0 → 23 (wrap-around)
		/// - For minutes: 0 → 59 and hour decremented
		/// - For seconds: 0 → 59 and minute decremented (hour may also decrement if minute was 0)
		/// 
		/// The result is clamped within valid time ranges (00:00:00 to 23:59:59).
		/// </summary>
		/// <Param name="sender">The source of the command execution event.</Param>
		/// <Param name="e">The command event arguments.</Param>
		private void OnDownExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			switch (_activeBox)
			{
				case eFocusedBox.Hour:
					Time = new TimeSpan((Time.Hours + 23) % 24, Time.Minutes, Time.Seconds);
					break;
				case eFocusedBox.Minute:
					if (Time.Minutes == 0)
					{
						int newHour = (Time.Hours + 23) % 24;
						Time = new TimeSpan(newHour, 59, Time.Seconds);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes - 1, Time.Seconds);
					}
					break;
				case eFocusedBox.Second:
					if (Time.Seconds == 0)
					{
						int newMinute = Time.Minutes;
						int newHour = Time.Hours;
						if (newMinute == 0)
						{
							newMinute = 59;
							newHour = (newHour + 23) % 24;
						}
						else
						{
							newMinute = newMinute - 1;
						}
						Time = new TimeSpan(newHour, newMinute, 59);
					}
					else
					{
						Time = new TimeSpan(Time.Hours, Time.Minutes, Time.Seconds - 1);
					}
					break;
			}
		}

		/// <summary>
		/// Updates the values in the text boxes to reflect the Time property.
		/// </summary>
		private void UpdateTextBoxes()
		{
			if (_hourBox != null)
				_hourBox.Text = Time.Hours.ToString("D2");
			if (_minBox != null)
				_minBox.Text = Time.Minutes.ToString("D2");
			if (_secBox != null)
				_secBox.Text = Time.Seconds.ToString("D2");
		}
	}
}