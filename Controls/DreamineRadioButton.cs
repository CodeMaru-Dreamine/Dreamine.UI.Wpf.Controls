using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineRadioButton
	/// \brief A custom RadioButton with built-in command binding functionality.
	/// 
	/// This control allows you to bind commands to interaction events like Click, MouseUp, KeyUp, or TouchUp
	/// using attached properties. XAML style is auto-merged without requiring App.xaml modification.
	/// </summary>
	public class DreamineRadioButton : RadioButton
	{
		/// <summary>
		/// Static constructor that overrides the default style and merges the style resource.
		/// </summary>
		static DreamineRadioButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineRadioButton),
				new FrameworkPropertyMetadata(typeof(DreamineRadioButton)));

			var uri = new Uri("/VsLibrary;component/UiComponent/Styles/DreamineRadioButtonStyle.xaml", UriKind.RelativeOrAbsolute);

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
		/// \brief Defines an <see cref="ICommand"/> to be executed when a specified event occurs.
		///
		/// Declared as an attached property to support command behavior from XAML.
		/// </summary>
		public new static readonly DependencyProperty CommandProperty =
		DependencyProperty.RegisterAttached(
			"Command",
			typeof(ICommand),
			typeof(DreamineRadioButton),
			new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \property CommandParameter
		/// \brief Parameter passed to the command when executed.
		///
		/// If not specified, the event arguments will be used as the default parameter.
		/// </summary>
		public new static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineRadioButton),
				new PropertyMetadata(null));

		/// <summary>
		/// \property CommandTriggerName
		/// \brief Specifies which event(s) will trigger command execution.
		///
		/// Example values: "Click", "PreviewMouseUp", "PreviewKeyUp", "TouchUp"
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
		DependencyProperty.RegisterAttached(
			"CommandTriggerName",
			typeof(string),
			typeof(DreamineRadioButton),
			new PropertyMetadata(null));

		/// <summary>
		/// Sets the CommandTriggerName property value.
		/// </summary>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/// <summary>
		/// Gets the CommandTriggerName property value.
		/// </summary>
		public static string GetCommandTriggerName(DependencyObject obj)
			=> (string)obj.GetValue(CommandTriggerNameProperty);

		/// <summary>
		/// Sets the Command property.
		/// </summary>
		public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

		/// <summary>
		/// Gets the Command property.
		/// </summary>
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/// <summary>
		/// Sets the CommandParameter property.
		/// </summary>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/// <summary>
		/// Gets the CommandParameter property.
		/// </summary>
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/// <summary>
		/// Registers event handlers for relevant UI events when the Command property changes.
		///
		/// Supported events:
		/// - PreviewMouseUp
		/// - MouseDoubleClick
		/// - PreviewKeyDown (internally mapped to PreviewKeyUp)
		/// - TouchUp
		/// - Click
		/// </summary>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is UIElement element)
			{
				element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "PreviewMouseUp", e);
				}), true);

				element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, e) =>
				{
					TryExecuteCommand(d, "MouseDoubleClick", e);
				}), true);

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
		/// Executes the command if the given event matches the CommandTriggerName.
		///
		/// Supports comma-separated multiple event names in CommandTriggerName.
		/// Falls back to event arguments if CommandParameter is not explicitly set.
		/// </summary>
		/// <Param name="d">The element that has the command and trigger properties assigned.</Param>
		/// <Param name="eventName">The name of the event that occurred (e.g., "Click", "MouseDoubleClick").</Param>
		/// <Param name="eventArgs">The event arguments associated with the triggered event.</Param>
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
