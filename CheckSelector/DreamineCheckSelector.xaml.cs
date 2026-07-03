using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls.VsCustomControls
{
	/// <summary>
	/// Interaction logic for DreamineCheckSelector.xaml.
	/// This control provides a checkbox with two associated radio buttons (Left/Right),
	/// along with a text input box for configuring the maximum axis value.
	/// </summary>
	public partial class DreamineCheckSelector : UserControl
	{
        // ---------------------------------------------------------------------
        // \brief Command
        // ---------------------------------------------------------------------

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(DreamineCheckSelector),
                new PropertyMetadata(null));

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
                
        // -- Remembers the last State of the Left/Right radio buttons
        private bool _lastLeftChecked = true;
		private bool _lastRightChecked = false;

		/// <summary>
		/// Event raised when any internal value (checkbox, radio, or textbox) changes.
		/// </summary>
		public event RoutedEventHandler ValueChanged = null!;

		/// <summary>
		/// Initializes a new Instance of the <see cref="DreamineCheckSelector"/> class.
		/// Subscribes to internal control events.
		/// </summary>
		public DreamineCheckSelector()
		{
			InitializeComponent();

			PART_CheckBox.Checked += (s, e) => OnCheckedChanged(true);
			PART_CheckBox.Unchecked += (s, e) => OnCheckedChanged(false);

			PART_RadioLeft.Checked += (s, e) => { RememberRadio(true); RaiseValueChanged(); };
			PART_RadioRight.Checked += (s, e) => { RememberRadio(false); RaiseValueChanged(); };

			PART_AxisMaxBox.LostFocus += (s, e) => RaiseValueChanged();
			PART_AxisMaxBox.KeyDown += (s, e) => { if (((KeyEventArgs)e).Key == Key.Enter) RaiseValueChanged(); };
		}

		/// <summary>
		/// Handles logic when the checkbox is checked or unchecked.
		/// Automatically restores or resets the radio button states.
		/// </summary>
		/// <Param name="isChecked">Indicates whether the checkbox is checked.</Param>
		private void OnCheckedChanged(bool isChecked)
		{
			if (isChecked)
			{
				if (IsLeftChecked || IsRightChecked)
				{
					_lastLeftChecked = IsLeftChecked;
					_lastRightChecked = IsRightChecked;
				}
				else
				{
					IsLeftChecked = _lastLeftChecked;
					IsRightChecked = _lastRightChecked;
				}
			}
			else
			{
				_lastLeftChecked = IsLeftChecked;
				_lastRightChecked = IsRightChecked;

				IsLeftChecked = false;
				IsRightChecked = false;
			}
			RaiseValueChanged();
		}

		/// <summary>
		/// Stores the last selected radio button State for restoration.
		/// </summary>
		/// <Param name="left">True if Left radio button is selected; otherwise false.</Param>
		private void RememberRadio(bool left)
		{
			_lastLeftChecked = left;
			_lastRightChecked = !left;
		}

        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event to notify external consumers of a State change.
        /// </summary>
        private void RaiseValueChanged()
        {
            // 기존 RoutedEvent
            ValueChanged?.Invoke(this, new RoutedEventArgs());

            // MVVM Command 실행
            if (Command?.CanExecute(this) == true)
                Command.Execute(this);
        }


        /// <summary>
        /// Gets or sets whether the checkbox is checked.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(DreamineCheckSelector), new PropertyMetadata(false));

		/// <inheritdoc cref="IsCheckedProperty"/>
		public bool IsChecked
		{
			get => (bool)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		/// <summary>
		/// Gets or sets whether the Left radio button is selected.
		/// </summary>
		public static readonly DependencyProperty IsLeftCheckedProperty =
			DependencyProperty.Register(nameof(IsLeftChecked), typeof(bool), typeof(DreamineCheckSelector), new PropertyMetadata(false));

		/// <inheritdoc cref="IsLeftCheckedProperty"/>
		public bool IsLeftChecked
		{
			get => (bool)GetValue(IsLeftCheckedProperty);
			set => SetValue(IsLeftCheckedProperty, value);
		}

		/// <summary>
		/// Gets or sets whether the Right radio button is selected.
		/// </summary>
		public static readonly DependencyProperty IsRightCheckedProperty =
			DependencyProperty.Register(nameof(IsRightChecked), typeof(bool), typeof(DreamineCheckSelector), new PropertyMetadata(false));

		/// <inheritdoc cref="IsRightCheckedProperty"/>
		public bool IsRightChecked
		{
			get => (bool)GetValue(IsRightCheckedProperty);
			set => SetValue(IsRightCheckedProperty, value);
		}

		/// <summary>
		/// Gets or sets the display text next to the checkbox.
		/// </summary>
		public static readonly DependencyProperty CheckTextProperty =
			DependencyProperty.Register(nameof(CheckText), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("체크"));

		/// <inheritdoc cref="CheckTextProperty"/>
		public string CheckText
		{
			get => (string)GetValue(CheckTextProperty);
			set => SetValue(CheckTextProperty, value);
		}

		/// <summary>
		/// Gets or sets the display text for the Left radio button.
		/// </summary>
		public static readonly DependencyProperty LeftRadioTextProperty =
			DependencyProperty.Register(nameof(LeftRadioText), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("LEFT"));

		/// <inheritdoc cref="LeftRadioTextProperty"/>
		public string LeftRadioText
		{
			get => (string)GetValue(LeftRadioTextProperty);
			set => SetValue(LeftRadioTextProperty, value);
		}

		/// <summary>
		/// Gets or sets the display text for the Right radio button.
		/// </summary>
		public static readonly DependencyProperty RightRadioTextProperty =
			DependencyProperty.Register(nameof(RightRadioText), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("RIGHT"));

		/// <inheritdoc cref="RightRadioTextProperty"/>
		public string RightRadioText
		{
			get => (string)GetValue(RightRadioTextProperty);
			set => SetValue(RightRadioTextProperty, value);
		}

		/// <summary>
		/// Gets or sets the group name for the radio buttons. 
		/// This ensures mutual exclusivity if multiple instances are on the same form.
		/// </summary>
		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("AxisSelectorGroup"));

		/// <inheritdoc cref="GroupNameProperty"/>
		public string GroupName
		{
			get => (string)GetValue(GroupNameProperty);
			set => SetValue(GroupNameProperty, value);
		}

		/// <summary>
		/// Gets or sets the maximum value of the axis as string. 
		/// This value is typically used for input binding from the textbox.
		/// </summary>
		public static readonly DependencyProperty AxisMaxProperty =
			DependencyProperty.Register(nameof(AxisMax), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("20"));

		/// <inheritdoc cref="AxisMaxProperty"/>
		public string AxisMax
		{
			get => (string)GetValue(AxisMaxProperty);
			set => SetValue(AxisMaxProperty, value);
		}
	}
}
