using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls.VsCustomControls
{
	/// <summary>
	/// \if KO
	/// <para>체크박스, 좌우 라디오 버튼과 축 최대값 입력을 하나로 묶은 선택 컨트롤입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a selector that combines a check box, left and right radio buttons, and an axis-maximum input.</para>
	/// \endif
	/// </summary>
	public partial class DreamineCheckSelector : UserControl
	{
        // ---------------------------------------------------------------------
        // \brief Command
        // ---------------------------------------------------------------------

        /// <summary>
        /// \if KO
        /// <para>내부 값이 변경될 때 실행할 명령 종속성 속성입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the command dependency property executed when an inner value changes.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(DreamineCheckSelector),
                new PropertyMetadata(null));

        /// <summary>
        /// \if KO
        /// <para>내부 값 변경 시 실행할 명령을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the command executed when an inner value changes.</para>
        /// \endif
        /// </summary>
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
                
        // -- Remembers the last State of the Left/Right radio buttons
        /// <summary>
        /// \if KO
        /// <para>마지막 좌측 라디오 선택 상태를 저장합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the last left-radio selection state.</para>
        /// \endif
        /// </summary>
        private bool _lastLeftChecked = true;
		/// <summary>
		/// \if KO
		/// <para>마지막 우측 라디오 선택 상태를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the last right-radio selection state.</para>
		/// \endif
		/// </summary>
		private bool _lastRightChecked = false;

		/// <summary>
		/// \if KO
		/// <para>내부 체크박스, 라디오 버튼 또는 텍스트 값이 변경될 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Occurs when an inner check box, radio button, or text value changes.</para>
		/// \endif
		/// </summary>
		public event RoutedEventHandler ValueChanged = null!;

		/// <summary>
		/// \if KO
		/// <para>구성 요소를 초기화하고 내부 컨트롤 이벤트를 구독합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes the component and subscribes to inner-control events.</para>
		/// \endif
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
		/// \if KO
		/// <para>체크 상태에 따라 마지막 라디오 선택을 복원하거나 현재 선택을 해제합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Restores the last radio selection or clears the current selection according to check state.</para>
		/// \endif
		/// </summary>
		/// <param name="isChecked">
		/// \if KO
		/// <para>체크박스가 선택되었는지 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Indicates whether the check box is selected.</para>
		/// \endif
		/// </param>
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
		/// \if KO
		/// <para>나중에 복원할 좌우 라디오 선택 상태를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores left and right radio selection state for later restoration.</para>
		/// \endif
		/// </summary>
		/// <param name="left">
		/// \if KO
		/// <para>왼쪽이 선택되었으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when the left option is selected.</para>
		/// \endif
		/// </param>
		private void RememberRadio(bool left)
		{
			_lastLeftChecked = left;
			_lastRightChecked = !left;
		}

        /// <summary>
        /// \if KO
        /// <para><see cref="ValueChanged"/> 이벤트를 발생시키고 구성된 명령을 현재 컨트롤과 함께 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Raises <see cref="ValueChanged"/> and executes the configured command with this control.</para>
        /// \endif
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
        /// \if KO
        /// <para>체크박스 선택 상태 종속성 속성입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the check-box selection-state dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(DreamineCheckSelector), new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>체크박스가 선택되어 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the check box is selected.</para>
		/// \endif
		/// </summary>
		public bool IsChecked
		{
			get => (bool)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>왼쪽 라디오 선택 상태 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the left-radio selection-state dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsLeftCheckedProperty =
			DependencyProperty.Register(nameof(IsLeftChecked), typeof(bool), typeof(DreamineCheckSelector), new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>왼쪽 라디오 버튼이 선택되어 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the left radio button is selected.</para>
		/// \endif
		/// </summary>
		public bool IsLeftChecked
		{
			get => (bool)GetValue(IsLeftCheckedProperty);
			set => SetValue(IsLeftCheckedProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>오른쪽 라디오 선택 상태 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the right-radio selection-state dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsRightCheckedProperty =
			DependencyProperty.Register(nameof(IsRightChecked), typeof(bool), typeof(DreamineCheckSelector), new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>오른쪽 라디오 버튼이 선택되어 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the right radio button is selected.</para>
		/// \endif
		/// </summary>
		public bool IsRightChecked
		{
			get => (bool)GetValue(IsRightCheckedProperty);
			set => SetValue(IsRightCheckedProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>체크박스 표시 텍스트 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the check-box display-text dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CheckTextProperty =
			DependencyProperty.Register(nameof(CheckText), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("체크"));

		/// <summary>
		/// \if KO
		/// <para>체크박스 옆에 표시할 텍스트를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the text displayed beside the check box.</para>
		/// \endif
		/// </summary>
		public string CheckText
		{
			get => (string)GetValue(CheckTextProperty);
			set => SetValue(CheckTextProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>왼쪽 라디오 표시 텍스트 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the left-radio display-text dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty LeftRadioTextProperty =
			DependencyProperty.Register(nameof(LeftRadioText), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("LEFT"));

		/// <summary>
		/// \if KO
		/// <para>왼쪽 라디오 버튼의 표시 텍스트를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the left radio button's display text.</para>
		/// \endif
		/// </summary>
		public string LeftRadioText
		{
			get => (string)GetValue(LeftRadioTextProperty);
			set => SetValue(LeftRadioTextProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>오른쪽 라디오 표시 텍스트 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the right-radio display-text dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty RightRadioTextProperty =
			DependencyProperty.Register(nameof(RightRadioText), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("RIGHT"));

		/// <summary>
		/// \if KO
		/// <para>오른쪽 라디오 버튼의 표시 텍스트를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the right radio button's display text.</para>
		/// \endif
		/// </summary>
		public string RightRadioText
		{
			get => (string)GetValue(RightRadioTextProperty);
			set => SetValue(RightRadioTextProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>라디오 버튼 상호 배타 그룹 이름 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the radio-button mutual-exclusion group-name dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("AxisSelectorGroup"));

		/// <summary>
		/// \if KO
		/// <para>라디오 버튼 그룹 이름을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the radio-button group name.</para>
		/// \endif
		/// </summary>
		public string GroupName
		{
			get => (string)GetValue(GroupNameProperty);
			set => SetValue(GroupNameProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>문자열 축 최대값 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the string-valued axis-maximum dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty AxisMaxProperty =
			DependencyProperty.Register(nameof(AxisMax), typeof(string), typeof(DreamineCheckSelector), new PropertyMetadata("20"));

		/// <summary>
		/// \if KO
		/// <para>텍스트 입력에 바인딩할 축 최대값 문자열을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the axis-maximum text bound to the input box.</para>
		/// \endif
		/// </summary>
		public string AxisMax
		{
			get => (string)GetValue(AxisMaxProperty);
			set => SetValue(AxisMaxProperty, value);
		}
	}
}
