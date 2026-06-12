using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineListBox  
	/// \brief Extended ListBox class that supports command execution on item selection.
	///
	/// This class inherits from the standard ListBox and internally uses attached properties  
	/// to execute an <c>ICommand</c> via <c>PreviewMouseUp</c> and <c>PreviewKeyDown</c> events.
	///
	/// Command binding can be applied in XAML as follows:
	/// \code{.xaml}
	/// <ListBox local:DreamineListBox.Command="{Binding ItemClickCommand}"
	///          local:DreamineListBox.CommandParameter="{Binding SelectedItem}" />
	/// \endcode
	/// </summary>
	public class DreamineListBox : ListBox
	{
        /// <summary>
        /// \brief Static constructor (type initializer).
        /// \details
        /// - Override DefaultStyleKey metadata ONCE per type.
        /// - Merge DreamineListBoxStyle.xaml into Application resources (only once).
        /// \note
        /// - Do NOT call OverrideMetadata in instance constructor.
        ///   It can crash when controls are created multiple times.
        /// </note>
        /// </summary>
        static DreamineListBox()
        {
            // \brief Default style key override (one-time per type)
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineListBox),
                new FrameworkPropertyMetadata(typeof(DreamineListBox)));

            // \brief Merge style dictionary once
            var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineListBoxStyle.xaml", UriKind.RelativeOrAbsolute);

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
        /// \brief Default constructor.
        /// \details
        /// - Keep it minimal.
        /// - Style is handled by static constructor.
        /// </details>
        /// </summary>
        public DreamineListBox()
        {
            TryMergeStyleOnce();
        }

        /// <summary>
		/// \brief DreamineListBoxStyle.xaml을 Application Resource에 1회만 병합한다.
		/// \note 디자인타임/테스트 환경에서 Application.Current가 null일 수 있으므로 null-safe 처리한다.
		/// </summary>
		private static void TryMergeStyleOnce()
        {
            var app = Application.Current;
            if (app == null)
                return;

            var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineListBoxStyle.xaml", UriKind.RelativeOrAbsolute);

            bool alreadyAdded =
                app.Resources.MergedDictionaries
                    .OfType<ResourceDictionary>()
                    .Any(x => x.Source != null && x.Source.Equals(uri));

            if (!alreadyAdded)
            {
                var dict = new ResourceDictionary { Source = uri };
                app.Resources.MergedDictionaries.Add(dict);
            }
        }


        /// <summary>
        /// \property Command
        /// \brief Defines an ICommand to be executed when a specified event occurs.
        ///
        /// This property is defined as an AttachedProperty and  
        /// executes the bound ICommand when events like Click, TouchUp, or PreviewKeyUp are triggered.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
								  DependencyProperty.RegisterAttached(
									  "Command",
									  typeof(ICommand),
									  typeof(DreamineListBox),
									  new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \property CommandParameter
		/// \brief The parameter to pass when executing the Command.
		///
		/// If this property is not set, the event argument (e.g., RoutedEventArgs or MouseButtonEventArgs)  
		/// will be passed automatically as the default parameter.
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineListBox),
				new PropertyMetadata(null));

		/// <summary>
		/// \property CommandTriggerName
		/// \brief The event name that determines when the Command should be executed.
		///
		/// For example: "Click", "PreviewMouseUp", "PreviewKeyUp", "TouchUp", etc.  
		/// The command will only be executed if the specified event name matches the actual triggered event.
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
		DependencyProperty.RegisterAttached(
			"CommandTriggerName",
			typeof(string),
			typeof(DreamineListBox),
			new PropertyMetadata(null));

		/// <summary>
		/// Sets the value of the CommandTriggerName property.
		/// </summary>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/// <summary>
		/// Gets the value of the CommandTriggerName property.
		/// </summary>
		public static string GetCommandTriggerName(DependencyObject obj)
			=> (string)obj.GetValue(CommandTriggerNameProperty);

		/// <summary>
		/// Sets the value of the Command attached property.
		/// </summary>
		/// <Param name="obj">The target DependencyObject.</Param>
		/// <Param name="value">The ICommand Instance to bind.</Param>
		public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

		/// <summary>
		/// Gets the value of the Command attached property.
		/// </summary>
		/// <Param name="obj">The target DependencyObject.</Param>
		/// <returns>The currently bound ICommand.</returns>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/// <summary>
		/// Sets the value of the CommandParameter attached property.
		/// </summary>
		/// <Param name="obj">The target DependencyObject.</Param>
		/// <Param name="value">The parameter to pass when executing the command.</Param>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/// <summary>
		/// Gets the value of the CommandParameter attached property.
		/// </summary>
		/// <Param name="obj">The target DependencyObject.</Param>
		/// <returns>The currently set command parameter object.</returns>
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/// <summary>
		/// Registers event handlers when the Command property is changed.
		///
		/// Supported internal events:
		/// - PreviewMouseUp (MouseButtonEventArgs)
		/// - MouseDoubleClick (MouseButtonEventArgs)
		/// - PreviewKeyDown (interpreted as PreviewKeyUp)
		/// - TouchUp (TouchEventArgs)
		/// - Click (RoutedEventArgs)
		///
		/// During runtime, only the events whose names match the `CommandTriggerName` will invoke the bound ICommand.
		/// </summary>
		/// <Param name="d">The DependencyObject to which the command is attached (usually a DreamineListBox or other UIElement).</Param>
		/// <Param name="e">The event arguments for the property change.</Param>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is UIElement element)
			{
				// 마우스 클릭
				element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "PreviewMouseUp", e);
				}), true);
				element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "MouseDoubleClick", e);
				}), true);

				// 키보드 입력
				element.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "PreviewKeyUp", e);
				}), true);

				element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, e) =>
				{
					TryExecuteCommand(d, "TouchUp", e);
				}));

				element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "Click", e);
				}));
			}
		}

		/// <summary>
		/// Executes the attached command if the given event name matches one of the values specified in `CommandTriggerName`.
		///
		/// The `CommandTriggerName` supports multiple values separated by commas.  
		/// If any value matches the actual event name, the command will be executed.
		///
		/// If `CommandParameter` is set, it will be passed to the command execution.  
		/// Otherwise, the event arguments (e.g., RoutedEventArgs, MouseButtonEventArgs) are used as the default parameter.
		/// </summary>
		/// <Param name="d">The object where the command and trigger properties are set.</Param>
		/// <Param name="eventName">The actual name of the event that occurred (e.g., "Click", "PreviewMouseUp").</Param>
		/// <Param name="eventArgs">The arguments for the event that triggered the command.</Param>
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
	}
}
