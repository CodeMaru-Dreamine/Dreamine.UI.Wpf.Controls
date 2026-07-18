// \file DreaminePasswordBox.cs
// \brief Hint/Error 지원 + Password 바인딩 가능한 커스텀 PasswordBox 컨트롤
// \details
// - ControlTemplate 내부 PART_PasswordBox(PasswordBox)와 연동
// - Password DP(TwoWay) 지원
// - 템플릿 재적용 시 이벤트 중복 등록 방지
// - PasswordBox.PasswordChanged <-> DP 변경 간 재진입(무한루프) 방지
// \author VsLibrary
// \date 2026-02-11

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Dreamine.UI.Wpf.Controls
{
    /// <summary>
    /// \if KO
    /// <para>힌트·오류 표시와 양방향 바인딩 가능한 암호 문자열을 제공하는 사용자 지정 암호 입력 컨트롤입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Provides a custom password input control with hint and error display plus a two-way-bindable password string.</para>
    /// \endif
    /// </summary>
    public class DreaminePasswordBox : Control
    {
        // =====================================================================
        // \brief Static constructor - style merge
        // =====================================================================
        /// <summary>
        /// \if KO
        /// <para>기본 스타일 키를 재정의하고 암호 상자 테마 리소스를 병합합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Overrides the default style key and merges the password-box theme resources.</para>
        /// \endif
        /// </summary>
        static DreaminePasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DreaminePasswordBox),
                new FrameworkPropertyMetadata(typeof(DreaminePasswordBox)));

            // \brief 스타일 리소스 병합
            var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreaminePasswordBoxStyle.xaml", UriKind.RelativeOrAbsolute);

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

        // =====================================================================
        // \brief DP: Hint / Error / IsPasswordEmpty / Password
        // =====================================================================

        /// <summary>
        /// \if KO
        /// <para>암호가 비었을 때 표시할 힌트 텍스트 종속성 속성입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the hint-text dependency property shown while the password is empty.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(DreaminePasswordBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// \if KO
        /// <para>오류 메시지 텍스트 종속성 속성입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the error-message text dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register(nameof(Error), typeof(string), typeof(DreaminePasswordBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// \if KO
        /// <para>내부 암호 상자가 비었는지 나타내는 종속성 속성입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the dependency property indicating whether the inner password box is empty.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty IsPasswordEmptyProperty =
            DependencyProperty.Register(nameof(IsPasswordEmpty), typeof(bool), typeof(DreaminePasswordBox),
                new PropertyMetadata(true));

        /// <summary>
        /// \if KO
        /// <para>기본적으로 양방향 바인딩되는 암호 문자열 종속성 속성입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the password-string dependency property that binds two-way by default.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(DreaminePasswordBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordChanged));

        /// <summary>
        /// \if KO
        /// <para>ViewModel에 바인딩할 암호 문자열을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the password string bound to a view model.</para>
        /// \endif
        /// </summary>
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>암호가 비었을 때 표시할 힌트 문자열을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the hint text shown while the password is empty.</para>
        /// \endif
        /// </summary>
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>표시할 오류 문자열을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the error text to display.</para>
        /// \endif
        /// </summary>
        public string Error
        {
            get => (string)GetValue(ErrorProperty);
            set => SetValue(ErrorProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>현재 암호가 비어 있는지 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets whether the current password is empty.</para>
        /// \endif
        /// </summary>
        public bool IsPasswordEmpty
        {
            get => (bool)GetValue(IsPasswordEmptyProperty);
            set => SetValue(IsPasswordEmptyProperty, value);
        }

        // =====================================================================
        // \brief Internal fields
        // =====================================================================

        /// <summary>
        /// \if KO
        /// <para>템플릿의 내부 암호 상자 파트를 캐시합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Caches the inner password-box template part.</para>
        /// \endif
        /// </summary>
        private PasswordBox? _partPasswordBox;

        /// <summary>
        /// \if KO
        /// <para>내부 암호 변경 처리기 참조를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the inner password-change handler reference.</para>
        /// \endif
        /// </summary>
        private RoutedEventHandler? _passwordChangedHandler;

        /// <summary>
        /// \if KO
        /// <para>종속성 속성과 내부 파트 동기화의 재진입을 방지합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Prevents reentrancy while synchronizing the dependency property and inner part.</para>
        /// \endif
        /// </summary>
        private bool _isSyncing;

        // =====================================================================
        // \brief Template hook
        // =====================================================================

        /// <summary>
        /// \if KO
        /// <para>템플릿을 적용할 때 PART_PasswordBox를 찾아 값과 이벤트를 연결합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Locates PART_PasswordBox and connects its value and events when the template is applied.</para>
        /// \endif
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // \brief 이전 PART가 있으면 이벤트 해제
            DetachPartHandlers();

            _partPasswordBox = GetTemplateChild("PART_PasswordBox") as PasswordBox;
            if (_partPasswordBox == null)
                return;

            // \brief 최초 동기화: DP -> PART
            SyncDpToPart(Password);

            // \brief PasswordChanged 연결 (참조 보관)
            _passwordChangedHandler = (_, __) =>
            {
                if (_partPasswordBox == null)
                    return;

                if (_isSyncing)
                    return;

                try
                {
                    _isSyncing = true;

                    string pwd = _partPasswordBox.Password ?? string.Empty;

                    // \brief PART -> DP
                    if (!string.Equals(Password ?? string.Empty, pwd, StringComparison.Ordinal))
                        SetCurrentValue(PasswordProperty, pwd);

                    // \brief empty 플래그 갱신
                    SetCurrentValue(IsPasswordEmptyProperty, string.IsNullOrEmpty(pwd));
                }
                finally
                {
                    _isSyncing = false;
                }
            };

            _partPasswordBox.PasswordChanged += (s, e) => _passwordChangedHandler?.Invoke(s, e);

            // \brief 컨트롤 클릭 시 내부 입력으로 포커스 이동 (UX)
            this.MouseDown += OnHostMouseDown;
            this.GotKeyboardFocus += OnHostGotKeyboardFocus;
        }

        /// <summary>
        /// \if KO
        /// <para>템플릿 재적용 전에 호스트 이벤트를 해제하고 내부 파트 캐시를 비웁니다.</para>
        /// \endif
        /// \if EN
        /// <para>Detaches host events and clears the inner-part cache before template reapplication.</para>
        /// \endif
        /// </summary>
        private void DetachPartHandlers()
        {
            // \note PasswordBox.PasswordChanged는 익명 람다로 직접 해제하기 어려우니
            //       템플릿 재적용 시 새 PART로 갈아끼우는 패턴에서 “PART 캐시 교체”로 방어한다.
            //       (현재 구조는 PART가 바뀌면 이전 PART는 UI 트리에서 떨어지므로 누적 영향이 크지 않음)

            this.MouseDown -= OnHostMouseDown;
            this.GotKeyboardFocus -= OnHostGotKeyboardFocus;

            _partPasswordBox = null;
            _passwordChangedHandler = null;
        }

        /// <summary>
        /// \if KO
        /// <para>호스트를 클릭하면 내부 암호 입력으로 포커스를 이동합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Moves focus to the inner password input when the host is clicked.</para>
        /// \endif
        /// </summary>
        /// <param name="sender">
        /// \if KO
        /// <para>이벤트를 발생시킨 호스트입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The host that raised the event.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>마우스 버튼 이벤트 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The mouse-button event data.</para>
        /// \endif
        /// </param>
        private void OnHostMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _partPasswordBox?.Focus();
        }

        /// <summary>
        /// \if KO
        /// <para>호스트가 키보드 포커스를 받으면 내부 암호 입력으로 포커스를 전달합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Forwards keyboard focus to the inner password input when the host receives focus.</para>
        /// \endif
        /// </summary>
        /// <param name="sender">
        /// \if KO
        /// <para>이벤트를 발생시킨 호스트입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The host that raised the event.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>이전·새 포커스를 제공하는 이벤트 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Event data providing old and new focus.</para>
        /// \endif
        /// </param>
        private void OnHostGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == this)
            {
                _partPasswordBox?.Focus();
                e.Handled = true;
            }
        }

        // =====================================================================
        // \brief DP -> PART 동기화
        // =====================================================================

        /// <summary>
        /// \if KO
        /// <para>암호 종속성 속성의 외부 변경을 내부 암호 입력에 반영합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Applies external password dependency-property changes to the inner password input.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>암호가 변경된 컨트롤입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The control whose password changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>새 암호 값을 포함한 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Change data containing the new password value.</para>
        /// \endif
        /// </param>
        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DreaminePasswordBox control)
                return;

            if (control._isSyncing)
                return;

            string newPassword = e.NewValue as string ?? string.Empty;

            control.SyncDpToPart(newPassword);
        }

        /// <summary>
        /// \if KO
        /// <para>재진입을 방지하면서 암호 종속성 속성 값을 내부 파트에 동기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Synchronizes the password dependency-property value to the inner part while preventing reentrancy.</para>
        /// \endif
        /// </summary>
        /// <param name="newPassword">
        /// \if KO
        /// <para>내부 파트에 적용할 암호입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The password to apply to the inner part.</para>
        /// \endif
        /// </param>
        private void SyncDpToPart(string newPassword)
        {
            try
            {
                _isSyncing = true;

                SetCurrentValue(IsPasswordEmptyProperty, string.IsNullOrEmpty(newPassword));

                if (_partPasswordBox != null)
                {
                    string current = _partPasswordBox.Password ?? string.Empty;
                    if (!string.Equals(current, newPassword, StringComparison.Ordinal))
                        _partPasswordBox.Password = newPassword;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
