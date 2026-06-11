// \file DreamineButton.cs
// \brief Custom button control for VsLibrary framework. Supports Icon/Shadow/Permission/AttachedCommand.

using CommunityToolkit.Mvvm.Input; // \brief IAsyncRelayCommand
using System;
using System.ComponentModel; // \brief DesignerProperties
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces.DependencyInjection;
using Dreamine.UI.Wpf.Controls.MessageBox;
using Dreamine.UI.Wpf.Controls.Navigation;

namespace Dreamine.UI.Wpf.Controls
{
	/**
	 * \class DreamineButton
	 * \brief Custom button control for VsLibrary framework.
	 *
	 * \details
	 * - Supports bitmap icons (ImageSource) and vector icons (IconPath)
	 * - Auto-merges Style ResourceDictionary
	 * - Trigger-based attached command execution (with reentrancy guard)
	 * - Login/permission gating by Grade / MinimumGrade / OnceLogin
	 * - Provides IconSize / IconMargin / IconStretch dependency properties
	 * - Provides UseShadow / ShadowBlurRadius / ShadowOpacity / ShadowDepth / ShadowDirection / ShadowColor dependency properties
	 */
	public class DreamineButton : Button
	{
		/**
		* \brief Selection visual presentation mode for DreamineButton.
		*/
		public enum eSelectedVisualMode
		{
			/**
			 * \brief Change border only.
			 */
			BorderOnly = 0,

			/**
			 * \brief Change background overlay only.
			 */
			BackgroundOnly = 1,

			/**
			 * \brief Change both border and background.
			 */
			Both = 2
		}

		/**
		 * \enum eIconPosition
		 * \brief Icon position.
		 */
		public enum eIconPosition { Left, Right, Top, Bottom, Full }

		/** \brief DI Container (resolved once in static constructor). */
		private static readonly Dreamine.MVVM.Interfaces.DependencyInjection.IDMContainer _container;

		/** \brief Snapshot of the current logged-in user. */
		private UserItem _user = null!;

		#region Template parts

		/** \brief Template part name for the shadow host. */
		private const string PART_ShadowHost = "ShadowHost";

		/** \brief Shadow host of the current template. */
		private Border? _shadowHost;

		#endregion

		#region New DPs : IconSize / IconMargin / IconStretch

		/**
		* \brief Controls the default icon size (Width/Height) at once.
		*/
		public double IconSize
		{
			get => (double)GetValue(IconSizeProperty);
			set => SetValue(IconSizeProperty, value);
		}

		/** \brief DP for \ref IconSize. */
		public static readonly DependencyProperty IconSizeProperty =
			DependencyProperty.Register(
				nameof(IconSize),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					20d,
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange |
					FrameworkPropertyMetadataOptions.AffectsRender));

		/**
		 * \brief Controls the icon margin.
		 */
		public Thickness IconMargin
		{
			get => (Thickness)GetValue(IconMarginProperty);
			set => SetValue(IconMarginProperty, value);
		}

		/** \brief DP for \ref IconMargin. */
		public static readonly DependencyProperty IconMarginProperty =
			DependencyProperty.Register(
				nameof(IconMargin),
				typeof(Thickness),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					new Thickness(0, 0, 4, 0),
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange));

		/**
		 * \brief Icon stretch mode.
		 */
		public Stretch IconStretch
		{
			get => (Stretch)GetValue(IconStretchProperty);
			set => SetValue(IconStretchProperty, value);
		}

		/** \brief DP for \ref IconStretch. */
		public static readonly DependencyProperty IconStretchProperty =
			DependencyProperty.Register(
				nameof(IconStretch),
				typeof(Stretch),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					Stretch.Uniform,
					FrameworkPropertyMetadataOptions.AffectsRender));

		#endregion

		#region New DPs : Shadow (Effect)

		/**
		 * \brief Enables or disables DropShadowEffect.
		 */
		public bool UseShadow
		{
			get => (bool)GetValue(UseShadowProperty);
			set => SetValue(UseShadowProperty, value);
		}

		/** \brief DP for \ref UseShadow. */
		public static readonly DependencyProperty UseShadowProperty =
			DependencyProperty.Register(
				nameof(UseShadow),
				typeof(bool),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					false,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow blur radius. */
		public double ShadowBlurRadius
		{
			get => (double)GetValue(ShadowBlurRadiusProperty);
			set => SetValue(ShadowBlurRadiusProperty, value);
		}

		/** \brief DP for \ref ShadowBlurRadius. */
		public static readonly DependencyProperty ShadowBlurRadiusProperty =
			DependencyProperty.Register(
				nameof(ShadowBlurRadius),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					18d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow opacity. */
		public double ShadowOpacity
		{
			get => (double)GetValue(ShadowOpacityProperty);
			set => SetValue(ShadowOpacityProperty, value);
		}

		/** \brief DP for \ref ShadowOpacity. */
		public static readonly DependencyProperty ShadowOpacityProperty =
			DependencyProperty.Register(
				nameof(ShadowOpacity),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					0.55d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow depth. */
		public double ShadowDepth
		{
			get => (double)GetValue(ShadowDepthProperty);
			set => SetValue(ShadowDepthProperty, value);
		}

		/** \brief DP for \ref ShadowDepth. */
		public static readonly DependencyProperty ShadowDepthProperty =
			DependencyProperty.Register(
				nameof(ShadowDepth),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					6d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/**
		 * \brief Shadow direction in degrees. 0=Left, 90=Down, 180=Right, 270=Up.
		 */
		public double ShadowDirection
		{
			get => (double)GetValue(ShadowDirectionProperty);
			set => SetValue(ShadowDirectionProperty, value);
		}

		/** \brief DP for \ref ShadowDirection. */
		public static readonly DependencyProperty ShadowDirectionProperty =
			DependencyProperty.Register(
				nameof(ShadowDirection),
				typeof(double),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					90d,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/** \brief Shadow color. */
		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}

		/** \brief DP for \ref ShadowColor. */
		public static readonly DependencyProperty ShadowColorProperty =
			DependencyProperty.Register(
				nameof(ShadowColor),
				typeof(Color),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					Colors.Black,
					FrameworkPropertyMetadataOptions.AffectsRender,
					OnShadowPropertyChanged));

		/**
		 * \brief Updates template Effect when any shadow DP changes.
		 */
		private static void OnShadowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineButton btn)
				btn.UpdateShadowEffect();
		}

		#endregion

		#region Existing : ImagePosition / OnceLogin / Login / IconSource / IconPath / Command etc.

		/**
		 * \brief Dependency property for selection visual presentation mode.
		 */
		public eSelectedVisualMode SelectedVisualMode
		{
			get => (eSelectedVisualMode)GetValue(SelectedVisualModeProperty);
			set => SetValue(SelectedVisualModeProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedVisualMode.
		 */
		public static readonly DependencyProperty SelectedVisualModeProperty =
			DependencyProperty.Register(
				nameof(SelectedVisualMode),
				typeof(eSelectedVisualMode),
				typeof(DreamineButton),
				new PropertyMetadata(eSelectedVisualMode.BorderOnly));

		/**
		 * \brief Selected border brush.
		 */
		public Brush SelectedBorderBrush
		{
			get => (Brush)GetValue(SelectedBorderBrushProperty);
			set => SetValue(SelectedBorderBrushProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedBorderBrush.
		 */
		public static readonly DependencyProperty SelectedBorderBrushProperty =
			DependencyProperty.Register(
				nameof(SelectedBorderBrush),
				typeof(Brush),
				typeof(DreamineButton),
				new PropertyMetadata(Brushes.Blue));

		/**
		 * \brief Selected border thickness.
		 */
		public Thickness SelectedBorderThickness
		{
			get => (Thickness)GetValue(SelectedBorderThicknessProperty);
			set => SetValue(SelectedBorderThicknessProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedBorderThickness.
		 */
		public static readonly DependencyProperty SelectedBorderThicknessProperty =
			DependencyProperty.Register(
				nameof(SelectedBorderThickness),
				typeof(Thickness),
				typeof(DreamineButton),
				new PropertyMetadata(new Thickness(3)));

		/**
		 * \brief Selected background overlay brush (alpha recommended).
		 * \details Template overlay uses this brush for BackgroundOnly/Both modes.
		 */
		public Brush SelectedBackgroundOverlay
		{
			get => (Brush)GetValue(SelectedBackgroundOverlayProperty);
			set => SetValue(SelectedBackgroundOverlayProperty, value);
		}

		/**
		 * \brief DP identifier for \ref SelectedBackgroundOverlay.
		 */
		public static readonly DependencyProperty SelectedBackgroundOverlayProperty =
			DependencyProperty.Register(
				nameof(SelectedBackgroundOverlay),
				typeof(Brush),
				typeof(DreamineButton),
				new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x30, 0x00, 0x7A, 0xFF))));

		/** \brief Icon layout position. */
		public eIconPosition ImagePosition
		{
			get => (eIconPosition)GetValue(ImagePositionProperty);
			set => SetValue(ImagePositionProperty, value);
		}

		/** \brief DP for \ref ImagePosition. */
		public static readonly DependencyProperty ImagePositionProperty =
			DependencyProperty.Register(
				nameof(ImagePosition),
				typeof(eIconPosition),
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(
					eIconPosition.Left,
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange));

		/** \brief Enables one-time login session reuse. */
		public bool OnceLogin
		{
			get => (bool)GetValue(OnceLoginProperty);
			set => SetValue(OnceLoginProperty, value);
		}

		/** \brief DP for \ref OnceLogin. */
		public static readonly DependencyProperty OnceLoginProperty =
			DependencyProperty.Register(
				nameof(OnceLogin),
				typeof(bool),
				typeof(DreamineButton),
				new PropertyMetadata(false, OnOnceLoginChanged));

		/**
		 * \brief Updates global navigation helper when OnceLogin changes.
		 */
		private static void OnOnceLoginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DreamineButton)
				VsNavigationHelper.IsOnceLoginEnabled = (bool)e.NewValue;
		}

		/**
		 * \brief Applies default style key and auto-merges ResourceDictionary.
		 */
		static DreamineButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(DreamineButton),
				new FrameworkPropertyMetadata(typeof(DreamineButton)));

			try
			{
				var uri = new Uri("/VsLibrary;component/UiComponent/Styles/DreamineButtonStyle.xaml",
								  UriKind.RelativeOrAbsolute);

				var app = Application.Current;
				if (app != null)
				{
					bool alreadyAdded = app.Resources.MergedDictionaries
						.OfType<ResourceDictionary>()
						.Any(x => x.Source != null && x.Source.Equals(uri));

					if (!alreadyAdded)
					{
						var dict = new ResourceDictionary { Source = uri };
						app.Resources.MergedDictionaries.Add(dict);
					}
				}
			}
			catch
			{
				// \brief Guard for design-time / missing resources, etc.
			}

			_container = VsContainer.Instance;
		}

		/**
		 * \brief Constructor: subscribes to login changed event.
		 */
		public DreamineButton()
		{
			// \brief Guard design-time (DI/VM access may crash designer).
			if (DesignerProperties.GetIsInDesignMode(this)) return;

			try
			{
				var loginControl = _container.Resolve<VsLoginControlViewModel>();
				loginControl.LoginChanged -= OnLoginChanged;
				loginControl.LoginChanged += OnLoginChanged;
			}
			catch
			{
				// \brief Safe guard for runtime environment / DI differences.
			}
		}

		/**
		 * \brief Caches template parts and updates shadow effect after template is applied.
		 */
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_shadowHost = GetTemplateChild(PART_ShadowHost) as Border;

			// \brief If template part name mismatches, _shadowHost can be null.
			UpdateShadowEffect();
		}

		/**
		 * \brief Applies shadow effect to ShadowHost.
		 */
		private void UpdateShadowEffect()
		{
			if (_shadowHost == null)
				return;

			if (!UseShadow)
			{
				_shadowHost.Effect = null;
				return;
			}

			// \brief Avoid background/brush interference: use Effect only on transparent host.
			_shadowHost.Effect = new DropShadowEffect
			{
				BlurRadius = ShadowBlurRadius,
				Opacity = ShadowOpacity,
				ShadowDepth = ShadowDepth,
				Direction = ShadowDirection,
				Color = ShadowColor
			};
		}

		/**
		 * \brief Handles login user changes.
		 */
		private void OnLoginChanged(UserItem? user)
		{
			if (user == null) return;

			_user = user;
			VsNavigationHelper.CurrentUser = user;
			if (OnceLogin) VsNavigationHelper.IsOnceLoginEnabled = true;
			Grade = _user.Grade;
		}

		/** \brief Bitmap icon source. */
		public ImageSource ImageSource
		{
			get => (ImageSource)GetValue(ImageSourceProperty);
			set => SetValue(ImageSourceProperty, value);
		}

		/** \brief DP for \ref ImageSource. */
		public static readonly DependencyProperty ImageSourceProperty =
			DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(DreamineButton),
				new PropertyMetadata(null));

		/** \brief Vector icon geometry (Path). */
		public Geometry IconPath
		{
			get => (Geometry)GetValue(IconPathProperty);
			set => SetValue(IconPathProperty, value);
		}

		/** \brief DP for \ref IconPath. */
		public static readonly DependencyProperty IconPathProperty =
			DependencyProperty.Register(nameof(IconPath), typeof(Geometry), typeof(DreamineButton),
				new PropertyMetadata(null));

		/** \brief Attached command (separate from Button.Command). */
		public new static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register(
				nameof(Command),
				typeof(ICommand),
				typeof(DreamineButton),
				new PropertyMetadata(null, OnCommandChanged));

		/** \brief Command get/set. */
		public new ICommand Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		/** \brief Attached command parameter. */
		public new static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.RegisterAttached(
				"CommandParameter",
				typeof(object),
				typeof(DreamineButton),
				new PropertyMetadata(null));

		/** \brief Sets CommandParameter attached property. */
		public static void SetCommandParameter(DependencyObject obj, object value)
			=> obj.SetValue(CommandParameterProperty, value);

		/** \brief Gets CommandParameter attached property. */
		public static object GetCommandParameter(DependencyObject obj)
			=> obj.GetValue(CommandParameterProperty);

		/** \brief Comma-separated trigger event names. */
		public static readonly DependencyProperty CommandTriggerNameProperty =
			DependencyProperty.RegisterAttached(
				"CommandTriggerName",
				typeof(string),
				typeof(DreamineButton),
				new PropertyMetadata("PreviewMouseUp"));

		/** \brief Sets CommandTriggerName attached property. */
		public static void SetCommandTriggerName(DependencyObject obj, string value)
			=> obj.SetValue(CommandTriggerNameProperty, value);

		/** \brief Gets CommandTriggerName attached property. */
		public static string GetCommandTriggerName(DependencyObject obj)
			=> (string)obj.GetValue(CommandTriggerNameProperty);

		/** \brief Attached Command helper set. */
		public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

		/** \brief Attached Command helper get. */
		public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

		/** \brief Prevents duplicate event handler hooking. */
		private static readonly DependencyProperty IsHandlersHookedProperty =
			DependencyProperty.RegisterAttached("IsHandlersHooked", typeof(bool), typeof(DreamineButton),
				new PropertyMetadata(false));

		/** \brief Gets IsHandlersHooked attached property. */
		private static bool GetIsHandlersHooked(DependencyObject d) => (bool)d.GetValue(IsHandlersHookedProperty);

		/** \brief Sets IsHandlersHooked attached property. */
		private static void SetIsHandlersHooked(DependencyObject d, bool v) => d.SetValue(IsHandlersHookedProperty, v);

		/** \brief Reentrancy guard flag. */
		private static readonly DependencyProperty IsExecutingProperty =
			DependencyProperty.RegisterAttached("IsExecuting", typeof(bool), typeof(DreamineButton),
				new PropertyMetadata(false));

		/** \brief Gets IsExecuting attached property. */
		private static bool GetIsExecuting(DependencyObject d) => (bool)d.GetValue(IsExecutingProperty);

		/** \brief Sets IsExecuting attached property. */
		private static void SetIsExecuting(DependencyObject d, bool v) => d.SetValue(IsExecutingProperty, v);

		/**
		 * \brief Hooks internal event handlers once when Command changes.
		 */
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not UIElement element) return;
			if (GetIsHandlersHooked(d)) return;

			element.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "PreviewMouseUp", ev);
			}), true);

			element.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "MouseDoubleClick", ev);
			}), true);

			element.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "PreviewKeyUp", ev);
			}), true);

			element.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, ev) =>
			{
				TryExecuteCommand(d, "TouchUp", ev);
			}));

			element.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, ev) =>
			{
				TryExecuteCommand(d, "Click", ev);
			}));

			SetIsHandlersHooked(d, true);
		}

		/**
 * \brief Executes the command only when the trigger name matches.
 */
		/// <summary>
		/// Tries to execute the bound command for a routed event trigger.
		/// Includes trigger filtering, re-entrancy guard, and optional login/permission handling for <see cref="DreamineButton"/>.
		/// </summary>
		/// <param name="d">The target <see cref="DependencyObject"/> that raised the event.</param>
		/// <param name="eventName">The routed event name.</param>
		/// <param name="eventArgs">The routed event arguments.</param>
		private static async void TryExecuteCommand(DependencyObject d, string eventName, RoutedEventArgs eventArgs)
		{
			await Task.Yield();

			var rawTrigger = GetCommandTriggerName(d);
			if (string.IsNullOrWhiteSpace(rawTrigger))
				return;

			var triggers = rawTrigger
				.Split(',')
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.ToArray();

			if (triggers.Length > 0 &&
				!triggers.Contains(eventName, StringComparer.OrdinalIgnoreCase))
				return;

			var command = GetCommand(d);

			/// <summary>
			/// \brief Resolve command parameter with a clear priority.
			/// \details
			/// Priority:
			/// 1) DreamineButton attached CommandParameter (DreamineButton.GetCommandParameter)
			/// 2) WPF ButtonBase.CommandParameter (standard DP)
			/// 3) RoutedEventArgs (fallback only when no parameter is supplied)
			/// </details>
			/// </summary>
			object? parameter = null;

			/// <summary>
			/// \brief 1) Attached CommandParameter (DreamineButton)
			/// \details If the attached property is not set, it returns null.
			/// </summary>
			parameter = GetCommandParameter(d);

			/// <summary>
			/// \brief 2) Standard WPF CommandParameter (ButtonBase)
			/// \details Supports XAML usage: CommandParameter="..."
			/// </summary>
			if (parameter == null && d is ButtonBase bbForParam)
				parameter = bbForParam.CommandParameter;

			/// <summary>
			/// \brief 3) Fallback to event args
			/// \details When no parameter is supplied, pass event args to allow advanced trigger scenarios.
			/// </summary>
			if (parameter == null)
				parameter = eventArgs;

			/// <summary>
			/// Prevent double invocation when ButtonBase.Command is already wired to the same ICommand.
			/// </summary>
			if (d is ButtonBase bb && command != null && ReferenceEquals(bb.Command, command))
				return;

			/// <summary>
			/// Re-entrancy guard.
			/// </summary>
			if (GetIsExecuting(d))
				return;

			SetIsExecuting(d, true);

			try
			{
				if (d is DreamineButton btn)
				{
					/// <summary>
					/// Applies the authenticated user to global context and button state.
					/// </summary>
					/// <param name="user">The authenticated user.</param>
					void ApplyUser(UserItem user)
					{
						VsNavigationHelper.CurrentUser = user;
						btn.Grade = user.Grade;
					}

					/// <summary>
					/// Returns true if the given user satisfies the minimum grade requirement.
					/// </summary>
					/// <param name="user">The user to validate.</param>
					/// <returns>True if user.Grade is sufficient; otherwise false.</returns>
					bool IsUserSufficient(UserItem user) => user.Grade >= btn.MinimumGrade;

					int currentGrade = VsNavigationHelper.CurrentUser?.Grade ?? 0;
					bool forceLogin = !VsNavigationHelper.IsOnceLoginEnabled;
					bool insufficient = currentGrade < btn.MinimumGrade;

					if (!forceLogin)
					{
						/// <summary>
						/// Non-forced login mode: block execution when the button grade is below requirement.
						/// </summary>
						if (btn.Grade < btn.MinimumGrade)
						{
							DreamineMessageBox.ShowAsync(
								"Please log in to continue.",
								"Login Required",
								autoClick: MessageBoxResult.OK,
								autoClickDelaySeconds: 3);
							return;
						}
					}
					else
					{
						/// <summary>
						/// Forced login mode: authenticate when login is forced or current grade is insufficient.
						/// </summary>
						if (forceLogin || insufficient)
						{
							if (!VsLoginControlViewModel.IsShow)
							{
								var user = await VsLoginControlViewModel.ShowLoginDialogAsync();

								/// <summary>
								/// Fallback to a default root account when the login UI is not used/available.
								/// NOTE: This policy must be explicitly allowed in your environment.
								/// </summary>
								if (user == null)
								{
									user = new UserItem
									{
										UserID = "root",
										Password = string.Empty,
										Grade = 2
									};
								}

								/// <summary>
								/// Block execution if the authenticated user does not satisfy the requirement.
								/// </summary>
								if (!IsUserSufficient(user))
									return;

								ApplyUser(user);
							}
						}
					}

					/// <summary>
					/// Notify navigation bar about the button click (if the DataContext is a ButtonData).
					/// </summary>
					if (btn.DataContext is ButtonData btnData)
						VsNavigationBar.NotifyButtonClicked(btnData);
				}

				if (command == null)
					return;

				/// <summary>
				/// Execute async command or sync command depending on the ICommand type.
				/// </summary>
				if (command is IAsyncRelayCommand asyncCmd)
				{
					if (asyncCmd.CanExecute(parameter))
						await asyncCmd.ExecuteAsync(parameter);
				}
				else
				{
					if (command.CanExecute(parameter))
						command.Execute(parameter);
				}

				eventArgs.Handled = true;
			}
			finally
			{
				SetIsExecuting(d, false);
			}
		}

		/** \brief Gradient top color. */
		public static readonly DependencyProperty BackgroundTopProperty =
			DependencyProperty.Register(nameof(BackgroundTop), typeof(Brush), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Shine top color. */
		public static readonly DependencyProperty ShineColorProperty =
			DependencyProperty.Register(nameof(ShineColor), typeof(Brush), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Shine bottom color. */
		public static readonly DependencyProperty ShineColorBottomProperty =
			DependencyProperty.Register(nameof(ShineColorBottom), typeof(Brush), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Gets/sets BackgroundTop. */
		public Brush BackgroundTop
		{
			get => (Brush)GetValue(BackgroundTopProperty);
			set => SetValue(BackgroundTopProperty, value);
		}

		/** \brief Gets/sets ShineColor. */
		public Brush ShineColor
		{
			get => (Brush)GetValue(ShineColorProperty);
			set => SetValue(ShineColorProperty, value);
		}

		/** \brief Gets/sets ShineColorBottom. */
		public Brush ShineColorBottom
		{
			get => (Brush)GetValue(ShineColorBottomProperty);
			set => SetValue(ShineColorBottomProperty, value);
		}

		/** \brief Selection state. */
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(DreamineButton), new PropertyMetadata(false));

		/** \brief Gets/sets IsSelected. */
		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		/** \brief Extended Focusable flag. */
		public static readonly DependencyProperty IsFocusableExProperty =
			DependencyProperty.Register(nameof(IsFocusableEx), typeof(bool), typeof(DreamineButton), new PropertyMetadata(true));

		/** \brief Gets/sets IsFocusableEx. */
		public bool IsFocusableEx
		{
			get => (bool)GetValue(IsFocusableExProperty);
			set => SetValue(IsFocusableExProperty, value);
		}

		/** \brief Focus restore target element. */
		public static readonly DependencyProperty RestoreFocusTargetProperty =
			DependencyProperty.Register(nameof(RestoreFocusTarget), typeof(IInputElement), typeof(DreamineButton), new PropertyMetadata(null));

		/** \brief Gets/sets RestoreFocusTarget. */
		public IInputElement? RestoreFocusTarget
		{
			get => (IInputElement?)GetValue(RestoreFocusTargetProperty);
			set => SetValue(RestoreFocusTargetProperty, value);
		}

		/** \brief Current user grade. */
		public static readonly DependencyProperty GradeProperty =
			DependencyProperty.Register(nameof(Grade), typeof(int), typeof(DreamineButton), new PropertyMetadata(0));

		/** \brief Gets/sets Grade. */
		public int Grade
		{
			get => (int)GetValue(GradeProperty);
			set => SetValue(GradeProperty, value);
		}

		/** \brief Minimum required grade. */
		public static readonly DependencyProperty MinimumGradeProperty =
			DependencyProperty.Register(nameof(MinimumGrade), typeof(int), typeof(DreamineButton), new PropertyMetadata(0));

		/** \brief Gets/sets MinimumGrade. */
		public int MinimumGrade
		{
			get => (int)GetValue(MinimumGradeProperty);
			set => SetValue(MinimumGradeProperty, value);
		}

		/**
		 * \brief Enables solid background mode.
		 */
		public static readonly DependencyProperty UseSolidBackgroundProperty =
			DependencyProperty.Register(
				nameof(UseSolidBackground),
				typeof(bool),
				typeof(DreamineButton),
				new PropertyMetadata(false));

		/** \brief Gets/sets UseSolidBackground. */
		public bool UseSolidBackground
		{
			get => (bool)GetValue(UseSolidBackgroundProperty);
			set => SetValue(UseSolidBackgroundProperty, value);
		}

		#endregion
	}
}
