// =====================================================================
// \file DreamineTextBox.cs
// \brief VsLibrary용 커스텀 TextBox. Hint/Error + CommandTrigger + Enter Focus UX 지원.
// =====================================================================

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls
{
    /// <summary>
    /// \class DreamineTextBox
    /// \brief Custom TextBox control tailored for VsLibrary.
    /// \details
    /// - Style 자동 Merge
    /// - Attached Command (PreviewMouseUp/MouseDoubleClick/PreviewKeyUp/TouchUp/Click/TextChanged)
    /// - Enter 입력 시 이전 포커스 대상으로 복귀(Enter Focus UX)
    /// </details>
    /// </summary>
    public class DreamineTextBox : TextBox
    {
        /// <summary>
        /// \brief 마지막으로 포커스를 가지고 있던 요소(Enter 복귀 대상).
        /// </summary>
        private IInputElement? _previousFocusedElement;

        /// <summary>
        /// \brief Static ctor: DefaultStyleKey + ResourceDictionary merge.
        /// </summary>
        static DreamineTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineTextBox),
                new FrameworkPropertyMetadata(typeof(DreamineTextBox)));

            var uri = new Uri("/VsLibrary;component/UiComponent/Styles/DreamineTextBoxStyle.xaml", UriKind.RelativeOrAbsolute);

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
        /// \brief ctor: 이전 포커스 저장을 "자기 자신" 이벤트에서 수행.
        /// \details
        /// - Window 전역 이벤트에 매달면 누수/중복/예외 케이스가 많음
        /// - GotKeyboardFocus의 OldFocus가 "진짜 이전 포커스"로 들어오는 편이라 안정적
        /// </details>
        /// </summary>
        public DreamineTextBox()
        {
            // \brief Enter 복귀를 위해 이전 포커스 저장
            GotKeyboardFocus += OnGotKeyboardFocus_StorePreviousFocus;
        }

        /// <summary>
        /// \brief GotKeyboardFocus에서 이전 포커스(OldFocus)를 저장한다.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">focus event args</param>
        private void OnGotKeyboardFocus_StorePreviousFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // \brief OldFocus가 Window/ContentControl로 들어오는 경우가 있으므로 교정
            var old = e.OldFocus;

            if (old is null)
            {
                _previousFocusedElement = null;
                return;
            }

            if (ReferenceEquals(old, this))
            {
                // \brief 자기 자신이면 의미 없음
                return;
            }

            if (old is DependencyObject dobj)
            {
                if (old is Window || old is ContentControl)
                {
                    var child = FindFirstFocusableChild(dobj);
                    _previousFocusedElement = child ?? old;
                    return;
                }
            }

            _previousFocusedElement = old;
        }

        /// <summary>
        /// \brief Enter 입력 시 이전 포커스 대상으로 복귀.
        /// \details
        /// - Dispatcher로 "키 입력 처리 이후"에 포커스 이동해야 안정적
        /// - Keyboard.ClearFocus()는 포커스를 날려버리므로 사용 금지
        /// </details>
        /// </summary>
        /// <param name="e">key event args</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key != Key.Enter)
                return;

            var target = _previousFocusedElement;

            if (target == null || ReferenceEquals(target, this))
                return;

            // \brief Enter 키 입력은 여기서 소비
            e.Handled = true;

            // \brief 이벤트 파이프라인/내부 TextBox 처리 후 포커스 이동
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // \brief UIElement이면 Focus()가 더 잘 먹는 경우가 많음
                    if (target is UIElement uie)
                    {
                        uie.Focus();
                        Keyboard.Focus(uie);
                        return;
                    }

                    Keyboard.Focus(target);
                }
                catch
                {
                    // \brief 포커스 실패는 치명적이지 않으므로 무시(정책에 맞게 로그로 바꿔도 됨)
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// \brief 첫 focusable child를 DFS로 탐색한다.
        /// </summary>
        /// <param name="parent">탐색 시작 parent</param>
        /// <returns>첫 focusable UIElement 또는 null</returns>
        private UIElement? FindFirstFocusableChild(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is UIElement uie && uie.Focusable && uie.IsVisible && uie.IsEnabled)
                    return uie;

                var result = FindFirstFocusableChild(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // =====================================================================
        // Attached Command: Command / CommandParameter / CommandTriggerName
        // =====================================================================

        /// <summary>
        /// \property Command
        /// \brief Attached ICommand.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(DreamineTextBox),
                new PropertyMetadata(null, OnCommandChanged));

        /// <summary>
        /// \property CommandParameter
        /// \brief Attached command parameter.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(DreamineTextBox),
                new PropertyMetadata(null));

        /// <summary>
        /// \property CommandTriggerName
        /// \brief Trigger event name list (comma-separated).
        /// </summary>
        public static readonly DependencyProperty CommandTriggerNameProperty =
            DependencyProperty.RegisterAttached(
                "CommandTriggerName",
                typeof(string),
                typeof(DreamineTextBox),
                new PropertyMetadata(null));

        /// <summary>
        /// \brief Set CommandTriggerName.
        /// </summary>
        public static void SetCommandTriggerName(DependencyObject obj, string value)
            => obj.SetValue(CommandTriggerNameProperty, value);

        /// <summary>
        /// \brief Get CommandTriggerName.
        /// </summary>
        public static string GetCommandTriggerName(DependencyObject obj)
            => (string)obj.GetValue(CommandTriggerNameProperty);

        /// <summary>
        /// \brief Set Command.
        /// </summary>
        public static void SetCommand(DependencyObject obj, ICommand value)
            => obj.SetValue(CommandProperty, value);

        /// <summary>
        /// \brief Get Command.
        /// </summary>
        public static ICommand GetCommand(DependencyObject obj)
            => (ICommand)obj.GetValue(CommandProperty);

        /// <summary>
        /// \brief Set CommandParameter.
        /// </summary>
        public static void SetCommandParameter(DependencyObject obj, object value)
            => obj.SetValue(CommandParameterProperty, value);

        /// <summary>
        /// \brief Get CommandParameter.
        /// </summary>
        public static object GetCommandParameter(DependencyObject obj)
            => obj.GetValue(CommandParameterProperty);

        /// <summary>
        /// \brief Command 변경 시 이벤트 훅 등록.
        /// \note (중요) 중복 훅 방지는 별도 플래그로 막는 것을 권장.
        /// </summary>
        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, args) =>
            {
                TryExecuteCommand(d, "PreviewMouseUp", args);
            }), true);

            element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, args) =>
            {
                TryExecuteCommand(d, "MouseDoubleClick", args);
            }), true);

            element.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, args) =>
            {
                // \brief 기존 규칙 유지: PreviewKeyDown 훅이지만 이름은 "PreviewKeyUp"
                TryExecuteCommand(d, "PreviewKeyUp", args);
            }), true);

            element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, args) =>
            {
                TryExecuteCommand(d, "TouchUp", args);
            }));

            element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, args) =>
            {
                TryExecuteCommand(d, "Click", args);
            }));

            element.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((s, args) =>
            {
                TryExecuteCommand(d, "TextChanged", args);
            }));
        }

        /// <summary>
        /// \brief TriggerName과 이벤트 이름이 매칭되면 Command 실행.
        /// </summary>
        private static void TryExecuteCommand(DependencyObject d, string eventName, RoutedEventArgs eventArgs)
        {
            var rawTrigger = GetCommandTriggerName(d);
            if (string.IsNullOrEmpty(rawTrigger))
                return;

            var triggerList = rawTrigger.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x));

            if (!triggerList.Contains(eventName, StringComparer.OrdinalIgnoreCase))
                return;

            var command = GetCommand(d);
            var parameter = GetCommandParameter(d) ?? eventArgs;

            if (command?.CanExecute(parameter) == true)
                command.Execute(parameter);
        }

        // =====================================================================
        // Hint / Error
        // =====================================================================

        /// <summary>
        /// \property Hint
        /// \brief Placeholder 문자열.
        /// </summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(DreamineTextBox), new PropertyMetadata(string.Empty));

        /// <summary>
        /// \brief Hint.
        /// </summary>
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary>
        /// \property Error
        /// \brief 에러 문자열(템플릿 표시용).
        /// </summary>
        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register(nameof(Error), typeof(string), typeof(DreamineTextBox), new PropertyMetadata(string.Empty));

        /// <summary>
        /// \brief Error.
        /// </summary>
        public string Error
        {
            get => (string)GetValue(ErrorProperty);
            set => SetValue(ErrorProperty, value);
        }
    }
}
