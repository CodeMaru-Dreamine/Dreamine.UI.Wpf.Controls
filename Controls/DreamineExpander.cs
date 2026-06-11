using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \class DreamineExpander
	/// \brief Custom expandable container with additional styling and command features for VsLibrary.
	///
	/// This control extends <see cref="HeaderedContentControl"/> and provides:
	/// - Custom styling without requiring App.xaml registration.
	/// - Command binding on expand/collapse State change.
	/// - Arrow icon positioning (Left/Right).
	/// - Expand animation toggle.
	/// </summary>
	public class DreamineExpander : HeaderedContentControl
	{
		/// <summary>
		/// Static constructor for <see cref="DreamineExpander"/>.
		/// 
		/// - Overrides the default style key to apply custom styles.
		/// - Automatically merges the DreamineExpander style XAML into application resources at runtime.
		///   This eliminates the need to manually register styles in App.xaml.
		/// - Ensures the resource dictionary is added only once to avoid duplication.
		/// </summary>
		static DreamineExpander()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineExpander),
				new FrameworkPropertyMetadata(typeof(DreamineExpander)));

			var uri = new Uri("/VsLibrary;component/UiComponent/Styles/DreamineExpanderStyle.xaml", UriKind.RelativeOrAbsolute);

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
		/// Gets or sets whether the expander is currently expanded.
		/// </summary>
		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		/// <summary>
		/// Identifies the IsExpanded dependency property.
		/// </summary>
		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(DreamineExpander),
				new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsExpandedChanged));

		/// <summary>
		/// Called when the IsExpanded property is changed.
		/// Executes the bound <see cref="ExpandChangedCommand"/> if present.
		/// </summary>
		private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineExpander expander)
			{
				if (expander.ExpandChangedCommand?.CanExecute(null) == true)
				{
					expander.ExpandChangedCommand.Execute(null);
				}
				else
				{
					if ((bool)expander.ShowExpandIcon == false && (bool)e.NewValue == false)
						expander.IsExpanded = true;
				}
			}
		}

		/// <summary>
		/// Gets or sets the command to execute when the expanded State changes.
		/// </summary>
		public ICommand ExpandChangedCommand
		{
			get => (ICommand)GetValue(ExpandChangedCommandProperty);
			set => SetValue(ExpandChangedCommandProperty, value);
		}

		/// <summary>
		/// Identifies the ExpandChangedCommand dependency property.
		/// </summary>
		public static readonly DependencyProperty ExpandChangedCommandProperty =
			DependencyProperty.Register(nameof(ExpandChangedCommand), typeof(ICommand), typeof(DreamineExpander));

		/// <summary>
		/// Gets or sets whether to use animation when expanding or collapsing.
		/// </summary>
		public bool UseExpandAnimation
		{
			get => (bool)GetValue(UseExpandAnimationProperty);
			set => SetValue(UseExpandAnimationProperty, value);
		}

		/// <summary>
		/// Identifies the UseExpandAnimation dependency property.
		/// </summary>
		public static readonly DependencyProperty UseExpandAnimationProperty =
			DependencyProperty.Register(nameof(UseExpandAnimation), typeof(bool), typeof(DreamineExpander), new PropertyMetadata(true));

		/// <summary>
		/// Gets or sets the position of the expand arrow icon (Left or Right).
		/// </summary>
		public eExpanderArrowPlacement ArrowPlacement
		{
			get => (eExpanderArrowPlacement)GetValue(ArrowPlacementProperty);
			set => SetValue(ArrowPlacementProperty, value);
		}

		/// <summary>
		/// Identifies the ArrowPlacement dependency property.
		/// </summary>
		public static readonly DependencyProperty ArrowPlacementProperty =
			DependencyProperty.Register(nameof(ArrowPlacement), typeof(eExpanderArrowPlacement), typeof(DreamineExpander),
				new PropertyMetadata(eExpanderArrowPlacement.Left));

		/// <summary>
		/// Gets or sets the font size used in the header.
		/// </summary>
		public double HeaderFontSize
		{
			get { return (double)GetValue(HeaderFontSizeProperty); }
			set { SetValue(HeaderFontSizeProperty, value); }
		}

		/// <summary>
		/// Identifies the HeaderFontSize dependency property.
		/// </summary>
		public static readonly DependencyProperty HeaderFontSizeProperty =
			DependencyProperty.Register("HeaderFontSize", typeof(double), typeof(DreamineExpander), new PropertyMetadata(SystemFonts.MessageFontSize));

		/// <summary>
		/// Gets or sets the font weight used in the header.
		/// </summary>
		public FontWeight HeaderFontWeight
		{
			get { return (FontWeight)GetValue(HeaderFontWeightProperty); }
			set { SetValue(HeaderFontWeightProperty, value); }
		}

		/// <summary>
		/// Identifies the HeaderFontWeight dependency property.
		/// </summary>
		public static readonly DependencyProperty HeaderFontWeightProperty =
			DependencyProperty.Register("HeaderFontWeight", typeof(FontWeight), typeof(DreamineExpander), new PropertyMetadata(FontWeights.Normal));

		/// <summary>
		/// Gets or sets the foreground brush of the header text.
		/// </summary>
		public Brush HeaderForeground
		{
			get { return (Brush)GetValue(HeaderForegroundProperty); }
			set { SetValue(HeaderForegroundProperty, value); }
		}

		/// <summary>
		/// Identifies the HeaderForeground dependency property.
		/// </summary>
		public static readonly DependencyProperty HeaderForegroundProperty =
			DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(DreamineExpander), new PropertyMetadata(SystemColors.ControlTextBrush));

		/// <summary>
		/// Gets or sets whether the expand/collapse icon is visible.
		/// </summary>
		public bool ShowExpandIcon
		{
			get => (bool)GetValue(ShowExpandIconProperty);
			set => SetValue(ShowExpandIconProperty, value);
		}

		/// <summary>
		/// Identifies the ShowExpandIcon dependency property.
		/// </summary>
		public static readonly DependencyProperty ShowExpandIconProperty =
			DependencyProperty.Register(nameof(ShowExpandIcon), typeof(bool), typeof(DreamineExpander),
				new PropertyMetadata(true));
	}

	/// <summary>
	/// \enum eExpanderArrowPlacement
	/// \brief Defines the alignment of the expand/collapse arrow icon in the <see cref="DreamineExpander"/>.
	/// </summary>
	public enum eExpanderArrowPlacement
	{
		/// <summary>
		/// Arrow is displayed on the Left side of the header.
		/// </summary>
		Left,

		/// <summary>
		/// Arrow is displayed on the Right side of the header.
		/// </summary>
		Right
	}
}