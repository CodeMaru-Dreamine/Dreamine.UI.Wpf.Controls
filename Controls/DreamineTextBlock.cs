using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineTextBlock
	/// \brief A custom TextBlock control used in VsLibrary.
	/// 
	/// Inherits from the default WPF TextBlock and provides automatic style application
	/// and resource merging. Designed to work without requiring manual style registration
	/// in App.xaml.
	/// </summary>
	public class DreamineTextBlock : TextBlock
	{
		/// <summary>
		/// Static constructor for the DreamineTextBlock class.
		///
		/// - Overrides the default style key to apply DreamineTextBlock-specific styles.
		/// - Merges the DreamineTextBlockStyle.xaml resource into the global resources to ensure
		///   automatic style application without requiring App.xaml registration.
		/// </summary>
		static DreamineTextBlock()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineTextBlock),
				new FrameworkPropertyMetadata(typeof(DreamineTextBlock)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineTextBlockStyle.xaml", UriKind.RelativeOrAbsolute);

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
		/// \property Command
		/// \brief Defines the ICommand to execute when the specified event is triggered.
		///
		/// This property is defined as an AttachedProperty and executes the bound ICommand  
		/// when events such as Click, TouchUp, or PreviewKeyUp occur.
		/// </summary>
		public static readonly DependencyProperty CommandProperty =
								  DependencyProperty.RegisterAttached(
									  "Command",
									  typeof(ICommand),
									  typeof(DreamineTextBlock),
									  new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \property CommandParameter
		/// \brief Specifies the parameter to pass when executing the Command.
		///
		/// If this property is not set, the event argument (e.g., RoutedEventArgs or MouseButtonEventArgs)  
		/// will be automatically passed to the command.
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineTextBlock),
				new PropertyMetadata(null));

		/// <summary>
		/// \property CommandTriggerName
		/// \brief Determines which event will trigger the execution of the associated ICommand.
		/// 
		/// Examples include: "Click", "PreviewMouseUp", "PreviewKeyUp", "TouchUp", etc.  
		/// The command will only execute if the specified event name matches the name of the raised event.
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
		DependencyProperty.RegisterAttached(
			"CommandTriggerName",
			typeof(string),
			typeof(DreamineTextBlock),
			new PropertyMetadata(null));

		/// <summary>
		/// Sets the value of the CommandTriggerName attached property.
		/// </summary>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/// <summary>
		/// Gets the value of the CommandTriggerName attached property.
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
		/// <returns>The currently assigned ICommand Instance.</returns>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/// <summary>
		/// Sets the CommandParameter attached property.
		/// </summary>
		/// <Param name="obj">The target DependencyObject.</Param>
		/// <Param name="value">The parameter to pass when executing the command.</Param>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/// <summary>
		/// Gets the CommandParameter attached property.
		/// </summary>
		/// <Param name="obj">The target DependencyObject.</Param>
		/// <returns>The parameter object set for command execution.</returns>
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/// <summary>
		/// Registers event handlers when the Command attached property is changed.
		///
		/// Internally supported events:
		/// - PreviewMouseUp (MouseButtonEventArgs)
		/// - PreviewKeyDown (handled as PreviewKeyUp)
		/// - TouchUp (TouchEventArgs)
		/// - Click (RoutedEventArgs)
		///
		/// During event invocation, the value of CommandTriggerName is compared to determine whether the command should execute.
		/// </summary>
		/// <Param name="d">The DependencyObject to which the command is attached (typically a DreamineTextBlock).</Param>
		/// <Param name="e">Information about the property change event.</Param>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is UIElement element)
			{
				// 마우스 클릭
				element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "PreviewMouseUp", e);
				}), true);
			}
		}

		/// <summary>
		/// Executes the bound ICommand if the specified event name matches any entry in the CommandTriggerName list.
		///
		/// CommandTriggerName supports multiple event names separated by commas (",").
		/// The command is executed only if the actual event name matches one of the listed trigger names.
		///
		/// Example: "Click,MouseDoubleClick,PreviewKeyUp"
		///
		/// If CommandParameter is explicitly set, it is passed as the command argument.  
		/// Otherwise, the event argument (e.g., RoutedEventArgs, MouseButtonEventArgs) is used as the parameter.
		/// </summary>
		/// <Param name="d">The object on which the command and trigger properties are set.</Param>
		/// <Param name="eventName">The actual name of the triggered event (e.g., "Click", "MouseDoubleClick").</Param>
		/// <Param name="eventArgs">The event argument associated with the triggered event.</Param>

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
