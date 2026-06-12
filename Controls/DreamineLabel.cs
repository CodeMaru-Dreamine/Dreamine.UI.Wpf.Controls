// \file DreamineLabel.cs
// \brief Custom Label control used in VsLibrary.
// \details
// - Auto style merge (DreamineLabelStyle.xaml).
// - Attached command execution via configurable triggers.
// - Prevents duplicate handler attachment using internal hooked flag.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineLabel
	/// \brief Custom Label control used in VsLibrary.
	/// \details
	/// - Auto merges style resource dictionary.
	/// - Supports attached command execution via trigger names (comma-separated).
	/// </details>
	/// </summary>
	public class DreamineLabel : Label
	{
		/// <summary>
		/// \brief Static constructor.
		/// \details
		/// - Overrides default style key.
		/// - Automatically merges DreamineLabelStyle.xaml into application resources.
		/// </details>
		/// </summary>
		static DreamineLabel()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineLabel),
				new FrameworkPropertyMetadata(typeof(DreamineLabel)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineLabelStyle.xaml", UriKind.RelativeOrAbsolute);

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
		// Attached Properties: Command / Parameter / Trigger
		// =====================================================================

		/// <summary>
		/// \brief Attached ICommand to execute when trigger event fires.
		/// </summary>
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.RegisterAttached(
				"Command",
				typeof(ICommand),
				typeof(DreamineLabel),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// \brief Attached command parameter.
		/// \details If null, event args will be passed.
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineLabel),
				new PropertyMetadata(null));

		/// <summary>
		/// \brief Attached trigger event name list (comma-separated).
		/// \details Example: "PreviewMouseUp,MouseDoubleClick,TouchUp"
		/// </summary>
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineLabel),
				new PropertyMetadata("PreviewMouseUp"));

		/// <summary>
		/// \brief Internal flag: prevent duplicate handler hookup.
		/// </summary>
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached(
				"IsHandlersHooked",
				typeof(bool),
				typeof(DreamineLabel),
				new PropertyMetadata(false));

		/// <summary>
		/// \brief Sets trigger names.
		/// </summary>
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/// <summary>
		/// \brief Gets trigger names.
		/// </summary>
		public static string GetCommandTriggerName(DependencyObject obj)
			=> obj.GetValue(CommandTriggerNameProperty) as string ?? string.Empty;

		/// <summary>
		/// \brief Sets command.
		/// </summary>
		public static void SetCommand(DependencyObject obj, ICommand value)
			=> obj.SetValue(CommandProperty, value);

		/// <summary>
		/// \brief Gets command.
		/// </summary>
		public static ICommand? GetCommand(DependencyObject obj)
			=> obj.GetValue(CommandProperty) as ICommand;

		/// <summary>
		/// \brief Sets command parameter.
		/// </summary>
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/// <summary>
		/// \brief Gets command parameter.
		/// </summary>
		public static object? GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/// <summary>
		/// \brief Hooks internal event handlers once when Command is first set.
		/// </summary>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not UIElement element)
				return;

			// \brief Prevent duplicate hooking
			bool hooked = (bool)(d.GetValue(IsHandlersHookedProperty) ?? false);
			if (hooked)
				return;

			d.SetValue(IsHandlersHookedProperty, true);

			// \brief Ensure hit-testable surface in common "label as button" usage
			if (d is Control c && c.Background == null)
				c.Background = Brushes.Transparent;

			// \brief Mouse up
			element.AddHandler(UIElement.PreviewMouseUpEvent,
				new MouseButtonEventHandler((s, args) => TryExecuteCommand(d, "PreviewMouseUp", args)),
				true);

			// \brief Double click (Label does not have MouseDoubleClick by default; Control.MouseDoubleClickEvent is routed)
			element.AddHandler(Control.MouseDoubleClickEvent,
				new MouseButtonEventHandler((s, args) => TryExecuteCommand(d, "MouseDoubleClick", args)),
				true);

			// \brief Touch up
			element.AddHandler(UIElement.TouchUpEvent,
				new EventHandler<TouchEventArgs>((s, args) => TryExecuteCommand(d, "TouchUp", args)));
		}

		/// <summary>
		/// \brief Executes ICommand if eventName is included in trigger list.
		/// </summary>
		private static void TryExecuteCommand(DependencyObject d, string eventName, RoutedEventArgs eventArgs)
		{
			var rawTrigger = GetCommandTriggerName(d);
			if (string.IsNullOrWhiteSpace(rawTrigger))
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
