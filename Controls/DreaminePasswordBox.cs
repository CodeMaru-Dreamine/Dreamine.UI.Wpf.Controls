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
    /// \class DreaminePasswordBox
    /// \brief Hint/Error 표시 + Password DP 바인딩을 제공하는 커스텀 패스워드 입력 컨트롤
    /// \details
    /// - 내부 템플릿에 PART_PasswordBox(PasswordBox)가 존재해야 함
    /// - PasswordProperty를 VM에 TwoWay로 바인딩 가능
    /// - 재진입 가드로 StackOverflow 방지
    /// </details>
    /// </summary>
    public class DreaminePasswordBox : Control
    {
        // =====================================================================
        // \brief Static constructor - style merge
        // =====================================================================
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

        /// <summary> \brief 비어있을 때 표시할 힌트 텍스트 </summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(DreaminePasswordBox),
                new PropertyMetadata(string.Empty));

        /// <summary> \brief 에러 메시지 표시 텍스트 </summary>
        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register(nameof(Error), typeof(string), typeof(DreaminePasswordBox),
                new PropertyMetadata(string.Empty));

        /// <summary> \brief 내부 PasswordBox가 비었는지 여부(힌트 표시 제어용) </summary>
        public static readonly DependencyProperty IsPasswordEmptyProperty =
            DependencyProperty.Register(nameof(IsPasswordEmpty), typeof(bool), typeof(DreaminePasswordBox),
                new PropertyMetadata(true));

        /// <summary>
        /// \brief 바인딩 가능한 패스워드 문자열 (TwoWay)
        /// \details VM에 TwoWay 바인딩하여 사용
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

        /// <summary> \brief Password 문자열 (DP) </summary>
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        /// <summary> \brief Hint 문자열 (DP) </summary>
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary> \brief Error 문자열 (DP) </summary>
        public string Error
        {
            get => (string)GetValue(ErrorProperty);
            set => SetValue(ErrorProperty, value);
        }

        /// <summary> \brief 비었는지 여부 (DP) </summary>
        public bool IsPasswordEmpty
        {
            get => (bool)GetValue(IsPasswordEmptyProperty);
            set => SetValue(IsPasswordEmptyProperty, value);
        }

        // =====================================================================
        // \brief Internal fields
        // =====================================================================

        /// <summary> \brief 템플릿 내부 PasswordBox 캐시 </summary>
        private PasswordBox? _partPasswordBox;

        /// <summary> \brief PasswordBox.PasswordChanged 핸들러 참조(중복등록/해제용) </summary>
        private RoutedEventHandler? _passwordChangedHandler;

        /// <summary> \brief 재진입(무한루프) 방지 플래그 </summary>
        private bool _isSyncing;

        // =====================================================================
        // \brief Template hook
        // =====================================================================

        /// <summary>
        /// \brief 템플릿 적용 시 PART_PasswordBox를 찾아 이벤트를 연결
        /// \details 템플릿이 다시 적용될 수 있으므로, 이전 핸들러를 해제하고 다시 연결한다.
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

        /// <summary> \brief 템플릿 재적용 대비: PART 및 이벤트 해제 </summary>
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

        /// <summary> \brief 컨트롤 클릭 시 PART로 포커스 </summary>
        private void OnHostMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _partPasswordBox?.Focus();
        }

        /// <summary> \brief 키보드 포커스 진입 시 PART로 포커스 </summary>
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
        /// \brief Password DP가 외부에서 변경되면 PART_PasswordBox에도 반영
        /// </summary>
        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DreaminePasswordBox control)
                return;

            if (control._isSyncing)
                return;

            string newPassword = e.NewValue as string ?? string.Empty;

            control.SyncDpToPart(newPassword);
        }

        /// <summary> \brief DP 값을 PART에 반영(재진입 가드 포함) </summary>
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
