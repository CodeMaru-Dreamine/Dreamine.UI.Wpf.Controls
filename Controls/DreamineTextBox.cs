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
    /// \if KO
    /// <para>스타일 병합, 연결 명령 및 Enter 키 포커스 복귀 기능을 제공하는 텍스트 상자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a text box that provides style merging, attached commands, and Enter-key focus restoration.</para>
    /// \endif
    /// </summary>
    public class DreamineTextBox : TextBox
    {
        /// <summary>
        /// \if KO
        /// <para>Enter 키를 누를 때 복귀할 이전 키보드 포커스 요소입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the previous keyboard-focus element to restore when Enter is pressed.</para>
        /// \endif
        /// </summary>
        private IInputElement? _previousFocusedElement;

        /// <summary>
        /// \if KO
        /// <para>기본 스타일 키를 등록하고 텍스트 상자 테마 리소스를 애플리케이션에 한 번 병합합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Registers the default style key and merges the text-box theme resource into the application once.</para>
        /// \endif
        /// </summary>
        static DreamineTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineTextBox),
                new FrameworkPropertyMetadata(typeof(DreamineTextBox)));

            var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineTextBoxStyle.xaml", UriKind.RelativeOrAbsolute);

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
        /// \if KO
        /// <para>새 인스턴스를 만들고 키보드 포커스 변경 이벤트를 구독합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance and subscribes to keyboard-focus changes.</para>
        /// \endif
        /// </summary>
        /// <remarks>
        /// \if KO
        /// <para>컨트롤 자체의 이벤트를 사용하여 창 단위 구독에 따른 중복과 수명 문제를 피합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Uses the control's own event to avoid duplication and lifetime issues caused by window-level subscriptions.</para>
        /// \endif
        /// </remarks>
        public DreamineTextBox()
        {
            // \brief Enter 복귀를 위해 이전 포커스 저장
            GotKeyboardFocus += OnGotKeyboardFocus_StorePreviousFocus;
        }

        /// <summary>
        /// \if KO
        /// <para>키보드 포커스를 얻기 직전의 유효한 포커스 대상을 저장합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the valid focus target that was active immediately before this control gained keyboard focus.</para>
        /// \endif
        /// </summary>
        /// <param name="sender">
        /// \if KO
        /// <para>이벤트를 발생시킨 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object that raised the event.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>이전 및 새 키보드 포커스 정보를 포함하는 이벤트 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Event data containing the old and new keyboard focus.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>Enter 키 입력을 처리한 뒤 이전 포커스 대상으로 비동기 복귀합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Asynchronously restores the previous focus target after handling the Enter key.</para>
        /// \endif
        /// </summary>
        /// <param name="e">
        /// \if KO
        /// <para>누른 키와 처리 상태를 포함하는 키 이벤트 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Key-event data containing the pressed key and handled state.</para>
        /// \endif
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="e"/>가 <see langword="null"/>일 때 기본 구현에서 발생할 수 있습니다.</para>
        /// \endif
        /// \if EN
        /// <para>May be thrown by the base implementation when <paramref name="e"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        /// <remarks>
        /// \if KO
        /// <para>포커스 이동 실패는 입력 흐름을 중단하지 않도록 의도적으로 무시합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Focus-movement failures are intentionally ignored so they do not interrupt input processing.</para>
        /// \endif
        /// </remarks>
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
        /// \if KO
        /// <para>시각적 자식 트리를 깊이 우선으로 탐색하여 첫 포커스 가능 요소를 찾습니다.</para>
        /// \endif
        /// \if EN
        /// <para>Searches the visual child tree depth-first for the first focusable element.</para>
        /// \endif
        /// </summary>
        /// <param name="parent">
        /// \if KO
        /// <para>탐색을 시작할 시각적 부모입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The visual parent at which to begin the search.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>표시되고 활성화된 첫 포커스 가능 요소이며, 없으면 <see langword="null"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The first visible, enabled, focusable element, or <see langword="null"/> if none exists.</para>
        /// \endif
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="parent"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="parent"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>이벤트가 일치할 때 실행할 연결 명령을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the attached command executed when an event matches.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(DreamineTextBox),
                new PropertyMetadata(null, OnCommandChanged));

        /// <summary>
        /// \if KO
        /// <para>연결 명령에 전달할 선택적 매개변수를 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the optional parameter passed to the attached command.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(DreamineTextBox),
                new PropertyMetadata(null));

        /// <summary>
        /// \if KO
        /// <para>명령을 실행할 쉼표 구분 이벤트 이름 목록을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the comma-separated event-name list that triggers the command.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty CommandTriggerNameProperty =
            DependencyProperty.RegisterAttached(
                "CommandTriggerName",
                typeof(string),
                typeof(DreamineTextBox),
                new PropertyMetadata(null));

        /// <summary>
        /// \if KO
        /// <para>지정한 객체의 명령 트리거 이름 목록을 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the command-trigger name list on the specified object.</para>
        /// \endif
        /// </summary>
        /// <param name="obj">
        /// \if KO
        /// <para>값을 설정할 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object on which to set the value.</para>
        /// \endif
        /// </param>
        /// <param name="value">
        /// \if KO
        /// <para>쉼표로 구분한 이벤트 이름 목록입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The comma-separated event-name list.</para>
        /// \endif
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        public static void SetCommandTriggerName(DependencyObject obj, string value)
            => obj.SetValue(CommandTriggerNameProperty, value);

        /// <summary>
        /// \if KO
        /// <para>지정한 객체의 명령 트리거 이름 목록을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the command-trigger name list from the specified object.</para>
        /// \endif
        /// </summary>
        /// <param name="obj">
        /// \if KO
        /// <para>값을 읽을 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object from which to read the value.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>쉼표로 구분한 이벤트 이름 목록입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The comma-separated event-name list.</para>
        /// \endif
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        public static string GetCommandTriggerName(DependencyObject obj)
            => (string)obj.GetValue(CommandTriggerNameProperty);

        /// <summary>
        /// \if KO
        /// <para>지정한 객체에 연결 명령을 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the attached command on the specified object.</para>
        /// \endif
        /// </summary>
        /// <param name="obj">
        /// \if KO
        /// <para>값을 설정할 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object on which to set the value.</para>
        /// \endif
        /// </param>
        /// <param name="value">
        /// \if KO
        /// <para>실행할 명령입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The command to execute.</para>
        /// \endif
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        public static void SetCommand(DependencyObject obj, ICommand value)
            => obj.SetValue(CommandProperty, value);

        /// <summary>
        /// \if KO
        /// <para>지정한 객체에 연결된 명령을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the command attached to the specified object.</para>
        /// \endif
        /// </summary>
        /// <param name="obj">
        /// \if KO
        /// <para>값을 읽을 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object from which to read the value.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>연결된 명령입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The attached command.</para>
        /// \endif
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// \if KO
        /// <para>저장된 값이 <see cref="ICommand"/>로 변환될 수 없을 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the stored value cannot be cast to <see cref="ICommand"/>.</para>
        /// \endif
        /// </exception>
        public static ICommand GetCommand(DependencyObject obj)
            => (ICommand)obj.GetValue(CommandProperty);

        /// <summary>
        /// \if KO
        /// <para>지정한 객체에 명령 매개변수를 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the command parameter on the specified object.</para>
        /// \endif
        /// </summary>
        /// <param name="obj">
        /// \if KO
        /// <para>값을 설정할 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object on which to set the value.</para>
        /// \endif
        /// </param>
        /// <param name="value">
        /// \if KO
        /// <para>명령에 전달할 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The value to pass to the command.</para>
        /// \endif
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        public static void SetCommandParameter(DependencyObject obj, object value)
            => obj.SetValue(CommandParameterProperty, value);

        /// <summary>
        /// \if KO
        /// <para>지정한 객체의 명령 매개변수를 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the command parameter from the specified object.</para>
        /// \endif
        /// </summary>
        /// <param name="obj">
        /// \if KO
        /// <para>값을 읽을 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object from which to read the value.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>구성된 명령 매개변수입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The configured command parameter.</para>
        /// \endif
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="obj"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="obj"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        public static object GetCommandParameter(DependencyObject obj)
            => obj.GetValue(CommandParameterProperty);

        /// <summary>
        /// \if KO
        /// <para>명령 속성이 변경되면 지원되는 입력 및 텍스트 이벤트 처리기를 등록합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Registers handlers for supported input and text events when the command property changes.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>명령 속성이 변경된 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object whose command property changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>이전 값과 새 값을 포함하는 속성 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Property-change data containing the old and new values.</para>
        /// \endif
        /// </param>
        /// <remarks>
        /// \if KO
        /// <para>현재 구현은 변경될 때마다 처리기를 추가하므로 호출 측에서 반복 설정을 피해야 합니다.</para>
        /// \endif
        /// \if EN
        /// <para>The current implementation adds handlers on every change, so callers should avoid repeated assignments.</para>
        /// \endif
        /// </remarks>
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
        /// \if KO
        /// <para>구성된 트리거 목록에 이벤트 이름이 포함되고 실행 가능하면 명령을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Executes the command when the configured trigger list contains the event name and the command can execute.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>연결 속성을 소유한 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object that owns the attached properties.</para>
        /// \endif
        /// </param>
        /// <param name="eventName">
        /// \if KO
        /// <para>비교할 이벤트 이름입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The event name to compare.</para>
        /// \endif
        /// </param>
        /// <param name="eventArgs">
        /// \if KO
        /// <para>명시적 매개변수가 없을 때 명령에 전달할 이벤트 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Event data passed to the command when no explicit parameter is configured.</para>
        /// \endif
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="d"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="d"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>자리표시자 문자열 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the placeholder-string dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(DreamineTextBox), new PropertyMetadata(string.Empty));

        /// <summary>
        /// \if KO
        /// <para>입력 안내용 자리표시자 문자열을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the placeholder string used as an input hint.</para>
        /// \endif
        /// </summary>
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>템플릿에 표시할 오류 문자열 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the error-string dependency property displayed by the template.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register(nameof(Error), typeof(string), typeof(DreamineTextBox), new PropertyMetadata(string.Empty));

        /// <summary>
        /// \if KO
        /// <para>템플릿에 표시할 오류 문자열을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the error string displayed by the template.</para>
        /// \endif
        /// </summary>
        public string Error
        {
            get => (string)GetValue(ErrorProperty);
            set => SetValue(ErrorProperty, value);
        }
    }
}
