// ============================================================================
// \file DreamineTabHost.SingleFile.cs
// \brief DreamineTabHost + DreamineTabHostItem (single-file copy/paste version)
// \details
//  - Allows XAML usage like:
//      <vs:DreamineTabHost>
//          <vs:DreamineTabHostItem Content="UiComponentDashboard"/>
//          <vs:DreamineTabHostItem Content="LocalizationDashboard"/>
//      </vs:DreamineTabHost>
//  - Achieved via [ContentProperty(nameof(Items))] so children go into Items,
//    not into Content (avoids "Content set more than once").
//  - Still supports ContentList / ContentTypeList DP-based inputs.
//  - Runtime view loading uses ViewLoader.LoadViewWithViewModel(...).
//  - Popup naming convention: "*_Popup".
//  - Visibility notification to root DataContext via IVisibilityAware.
// \date 2026-02-12
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces;
using Dreamine.UI.Wpf.Controls.ViewRegion;

namespace Dreamine.UI.Wpf.Controls
{
    /// <summary>
    /// \class DreamineTabHost
    /// \brief Content control that dynamically loads and switches views by string keys.
    /// Details:
    ///  - IMPORTANT: XAML children are mapped to <see cref="Items"/> (NOT Content) via <see cref="ContentPropertyAttribute"/>.
    ///  - This enables:
    ///      <DreamineTabHost>
    ///          &lt;DreamineTabHostItem Content="A"/&gt;
    ///          &lt;DreamineTabHostItem Content="B"/&gt;
    ///      </DreamineTabHost>
    ///  - Views can be declared by:
    ///    1) XAML children (<see cref="Items"/>)
    ///    2) <see cref="ContentList"/> / <see cref="ContentTypeList"/> DPs
    ///  - Popup identification:
    ///    - Names ending with "_Popup" are treated as popups.
    ///  - When <see cref="UseSingletonView"/> is true, views are registered to RegionManager and reused.
    ///  - Design-time:
    ///    - No DI / RegionManager / PopupService usage.
    ///    - Views created by Activator and can auto-wire design VM.
    /// </summary>
    [ContentProperty(nameof(Items))]
    public class DreamineTabHost : ContentControl
    {
        /// <summary>
        /// \brief XAML child items collection.
        /// Details:
        ///  - XAML children &lt;DreamineTabHostItem .../&gt; are added here automatically.
        ///  - This is NOT a DependencyProperty intentionally (to keep parsing simple and stable).
        /// </summary>
        public ObservableCollection<DreamineTabHostItem> Items { get; } = new ObservableCollection<DreamineTabHostItem>();

        /// <summary>
        /// \brief Last active root view instance (non-popup).
        /// </summary>
        private FrameworkElement? _currentView;

        /// <summary>
        /// \brief Runtime/design view map keyed by logical view name.
        /// </summary>
        private readonly Dictionary<string, FrameworkElement> _views = new Dictionary<string, FrameworkElement>();

        /// <summary>
        /// \brief Ordered logical key list for index-based switching.
        /// </summary>
        private readonly List<string> _order = new List<string>();

        /// <summary>
        /// \brief Popup manager (singleton).
        /// </summary>
        private readonly PopupWindowManager _popupManager = PopupWindowManager.Instance;

        /// <summary>
        /// \brief Switch helper.
        /// </summary>
        private ViewSwitcher? _viewSwitcher;

        /// <summary>
        /// \brief Visual host for current views.
        /// </summary>
        private Grid _containerGrid = null!;

        /// <summary>
        /// \brief Coalesce switch requests into one dispatcher tick.
        /// </summary>
        private bool _isSwitchScheduled;

        /// <summary>
        /// \brief Design-time last selected index.
        /// </summary>
        private int _designLastIndex = -1;

        /// <summary>
        /// \brief Cache last visibility state per root view to avoid duplicate callbacks.
        /// </summary>
        private readonly Dictionary<FrameworkElement, bool> _lastVisibilityState = new Dictionary<FrameworkElement, bool>();

        #region ===== Popup awareness: Flag DP & Readonly DP =====

        /// <summary>
        /// \brief Optional flag to compute/expose popup-ness before switching.
        /// </summary>
        public static readonly DependencyProperty TargetAnalyzedProperty =
            DependencyProperty.Register(
                nameof(TargetAnalyzed),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(false));

        /// <summary>
        /// \brief Gets or sets whether to compute and expose popup-ness before switching.
        /// </summary>
        public bool TargetAnalyzed
        {
            get => (bool)GetValue(TargetAnalyzedProperty);
            set => SetValue(TargetAnalyzedProperty, value);
        }

        /// <summary>
        /// \brief Read-only DP key for IsCurrentTargetPopup.
        /// </summary>
        private static readonly DependencyPropertyKey IsCurrentTargetPopupPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsCurrentTargetPopup),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(false));

        /// <summary>
        /// \brief Read-only DP that indicates whether the pending target is a popup.
        /// </summary>
        public static readonly DependencyProperty IsCurrentTargetPopupProperty =
            IsCurrentTargetPopupPropertyKey.DependencyProperty;

        /// <summary>
        /// \brief Gets whether the pending switch target is a popup.
        /// </summary>
        public bool IsCurrentTargetPopup
        {
            get => (bool)GetValue(IsCurrentTargetPopupProperty);
            private set => SetValue(IsCurrentTargetPopupPropertyKey, value);
        }

        #endregion

        /// <summary>
        /// \brief Initializes a new instance.
        /// </summary>
        public DreamineTabHost()
        {
            // \brief Keep default DP collections alive
            ContentList = new ObservableCollection<string>();

            // \brief When XAML children change, re-init
            Items.CollectionChanged += (_, __) => TryInitViews();
        }

        // =====================================================================
        // Dependency Properties (Public)
        // =====================================================================

        /// <summary>
        /// \brief DP backing field for ContentList.
        /// </summary>
        public static readonly DependencyProperty ContentListProperty =
            DependencyProperty.Register(
                nameof(ContentList),
                typeof(ObservableCollection<string>),
                typeof(DreamineTabHost),
                new PropertyMetadata(new ObservableCollection<string>(), OnContentInputsChanged));

        /// <summary>
        /// \brief Logical list of view names or type names.
        /// Details:
        ///  - Can contain short type names or fully qualified type names.
        /// </summary>
        public ObservableCollection<string> ContentList
        {
            get => (ObservableCollection<string>)GetValue(ContentListProperty);
            set => SetValue(ContentListProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for ContentTypeList.
        /// </summary>
        public static readonly DependencyProperty ContentTypeListProperty =
            DependencyProperty.Register(
                nameof(ContentTypeList),
                typeof(ObservableCollection<string>),
                typeof(DreamineTabHost),
                new PropertyMetadata(null, OnContentInputsChanged));

        /// <summary>
        /// \brief Optional list of concrete view type names (matched by index with ContentList).
        /// </summary>
        public ObservableCollection<string> ContentTypeList
        {
            get => (ObservableCollection<string>)GetValue(ContentTypeListProperty);
            set => SetValue(ContentTypeListProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for CurrentView.
        /// </summary>
        public static readonly DependencyProperty CurrentViewProperty =
            DependencyProperty.Register(
                nameof(CurrentView),
                typeof(string),
                typeof(DreamineTabHost),
                new PropertyMetadata(string.Empty, OnCurrentViewChanged));

        /// <summary>
        /// \brief Current view logical name (string-based navigation).
        /// </summary>
        public string CurrentView
        {
            get => (string)GetValue(CurrentViewProperty);
            set => SetValue(CurrentViewProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for SelectView.
        /// </summary>
        public static readonly DependencyProperty SelectViewProperty =
            DependencyProperty.Register(
                nameof(SelectView),
                typeof(int),
                typeof(DreamineTabHost),
                new PropertyMetadata(0, OnSelectViewChanged));

        /// <summary>
        /// \brief Current view index (0-based).
        /// </summary>
        public int SelectView
        {
            get => (int)GetValue(SelectViewProperty);
            set => SetValue(SelectViewProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for UseSingletonView.
        /// </summary>
        public static readonly DependencyProperty UseSingletonViewProperty =
            DependencyProperty.Register(
                nameof(UseSingletonView),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(true, OnContentInputsChanged));

        /// <summary>
        /// \brief Whether views are reused as singletons.
        /// </summary>
        public bool UseSingletonView
        {
            get => (bool)GetValue(UseSingletonViewProperty);
            set => SetValue(UseSingletonViewProperty, value);
        }

        // ----- Design hints -----

        /// <summary>
        /// \brief DP backing field for DesignBaseNamespaceHint.
        /// </summary>
        public static readonly DependencyProperty DesignBaseNamespaceHintProperty =
            DependencyProperty.Register(
                nameof(DesignBaseNamespaceHint),
                typeof(string),
                typeof(DreamineTabHost),
                new PropertyMetadata(string.Empty, OnDesignHintChanged));

        /// <summary>
        /// \brief Base namespace hint for design-time short name resolution.
        /// </summary>
        public string DesignBaseNamespaceHint
        {
            get => (string)GetValue(DesignBaseNamespaceHintProperty);
            set => SetValue(DesignBaseNamespaceHintProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for DesignAssemblyHint.
        /// </summary>
        public static readonly DependencyProperty DesignAssemblyHintProperty =
            DependencyProperty.Register(
                nameof(DesignAssemblyHint),
                typeof(string),
                typeof(DreamineTabHost),
                new PropertyMetadata(string.Empty, OnDesignHintChanged));

        /// <summary>
        /// \brief Assembly hint for design-time short name resolution.
        /// </summary>
        public string DesignAssemblyHint
        {
            get => (string)GetValue(DesignAssemblyHintProperty);
            set => SetValue(DesignAssemblyHintProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for AutoWireDesignViewModel.
        /// </summary>
        public static readonly DependencyProperty AutoWireDesignViewModelProperty =
            DependencyProperty.Register(
                nameof(AutoWireDesignViewModel),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(true, OnDesignHintChanged));

        /// <summary>
        /// \brief Whether to attach a design-time ViewModel if DataContext is null.
        /// </summary>
        public bool AutoWireDesignViewModel
        {
            get => (bool)GetValue(AutoWireDesignViewModelProperty);
            set => SetValue(AutoWireDesignViewModelProperty, value);
        }

        // =====================================================================
        // DP Callbacks
        // =====================================================================

        /// <summary>
        /// \brief Re-init when content inputs change.
        /// </summary>
        private static void OnContentInputsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DreamineTabHost c)
            {
                c.TryInitViews();
            }
        }

        /// <summary>
        /// \brief Re-init when design hints change (design-mode only).
        /// </summary>
        private static void OnDesignHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DreamineTabHost c && c.IsInDesignMode())
            {
                c.TryInitViews();
            }
        }

        /// <summary>
        /// \brief Handle CurrentView changes.
        /// </summary>
        private static void OnCurrentViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DreamineTabHost ctrl && e.NewValue is string viewName)
            {
                ctrl.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ctrl.SwitchToViewInternal(viewName);
                }), DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// \brief Handle SelectView changes by scheduling switch.
        /// </summary>
        private static void OnSelectViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DreamineTabHost c)
            {
                c.ScheduleSwitch();
            }
        }

        // =====================================================================
        // Lifecycle
        // =====================================================================

        /// <summary>
        /// \brief Initialize after XAML parse is done.
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // \brief Ensure initialization after XAML children (Items) are populated.
            Dispatcher.InvokeAsync(() =>
            {
                TryInitViews();
            }, DispatcherPriority.ContextIdle);
        }

        // =====================================================================
        // Switching
        // =====================================================================

        /// <summary>
        /// \brief Coalesce rapid selection changes into a single tick.
        /// </summary>
        private void ScheduleSwitch()
        {
            if (_isSwitchScheduled)
            {
                return;
            }

            _isSwitchScheduled = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (IsInDesignMode())
                    {
                        UpdateVisibleViewDesign();
                    }
                    else
                    {
                        UpdateVisibleView();
                    }
                }
                finally
                {
                    _isSwitchScheduled = false;
                }
            }), DispatcherPriority.Input);
        }

        /// <summary>
        /// \brief Internal view switch by logical key.
        /// </summary>
        /// <param name="targetKey">Logical view key.</param>
        private void SwitchToViewInternal(string? targetKey)
        {
            if (_viewSwitcher == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetKey))
            {
                return;
            }

            bool isPopup = _viewSwitcher.IsPopupName(targetKey);

            if (TargetAnalyzed)
            {
                IsCurrentTargetPopup = isPopup;
            }

            // \note IMPORTANT:
            //  - We intentionally call Switch(key) WITHOUT extra named parameters,
            //    because your ViewSwitcher signature currently does not accept targetAnalyzed (CS1739).
            _viewSwitcher.Switch(targetKey, isPopup);

            if (!isPopup && _views.TryGetValue(targetKey, out var nextView))
            {
                _currentView = nextView;
            }
        }

        /// <summary>
        /// \brief Runtime switch by SelectView index.
        /// </summary>
        private void UpdateVisibleView()
        {
            if (_viewSwitcher == null)
            {
                return;
            }

            // \brief Prefer ordered keys (stable), not Dictionary iteration.
            string? targetKey = _order.ElementAtOrDefault(SelectView);

            // \brief If missing, fallback: if the original declared entry is "*_Popup", map it.
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                var raw = GetDeclaredRawEntryAt(SelectView);
                if (!string.IsNullOrWhiteSpace(raw) &&
                    raw.EndsWith("_Popup", StringComparison.OrdinalIgnoreCase))
                {
                    string withoutSuffix = raw[..^6];
                    targetKey = DeriveSimpleKeyFromType(withoutSuffix);
                }
            }

            if (string.IsNullOrWhiteSpace(targetKey))
            {
                return;
            }

            SwitchToViewInternal(targetKey);
        }

        /// <summary>
        /// \brief Design-time switch by swapping the single child for O(1) switching.
        /// </summary>
        private void UpdateVisibleViewDesign()
        {
            if (_containerGrid == null || _views.Count == 0)
            {
                return;
            }

            int newIndex = SelectView;
            if (newIndex < 0) newIndex = 0;
            if (newIndex >= _order.Count) newIndex = _order.Count - 1;

            if (newIndex == _designLastIndex && _containerGrid.Children.Count == 1)
            {
                return;
            }

            var newKey = _order.ElementAtOrDefault(newIndex);
            if (string.IsNullOrWhiteSpace(newKey))
            {
                return;
            }

            var newView = _views[newKey];

            _containerGrid.Children.Clear();
            _containerGrid.Children.Add(newView);

            _designLastIndex = newIndex;
        }

        // =====================================================================
        // Visibility aware
        // =====================================================================

        /// <summary>
        /// \brief Handles IsVisibleChanged on the root view to call IVisibilityAware hooks.
        /// </summary>
        private void OnRootIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not FrameworkElement fe)
            {
                return;
            }

            if (fe.DataContext is not IVisibilityAware vm)
            {
                return;
            }

            bool isVisible = (bool)e.NewValue;

            if (_lastVisibilityState.TryGetValue(fe, out bool last) && last == isVisible)
            {
                return;
            }

            _lastVisibilityState[fe] = isVisible;

            if (isVisible)
            {
                vm.OnShown();
            }
            else
            {
                vm.OnHidden();
            }
        }

        // =====================================================================
        // Initialization
        // =====================================================================

        /// <summary>
        /// \brief Initializes and materializes views based on Items / ContentList / ContentTypeList.
        /// </summary>
        private void TryInitViews()
        {
            // \brief Prepare container
            _containerGrid ??= new Grid();
            Content = _containerGrid;

            _containerGrid.Children.Clear();
            _views.Clear();
            _order.Clear();
            _currentView = null;
            _lastVisibilityState.Clear();

            // \brief Build pairs from:
            //  1) Items (XAML children) if present
            //  2) otherwise DP collections (ContentList/ContentTypeList)
            var pairs = BuildPairsFromItemsOrDps();

            if (pairs.Count == 0)
            {
                return;
            }

            if (IsInDesignMode())
            {
                InitDesignViews(pairs);
                UpdateVisibleViewDesign();
                return;
            }

            // \brief Runtime initialization
            foreach (var (name, typeName) in pairs)
            {
                var result = ViewLoader.LoadViewWithViewModel(typeName, UseSingletonView);

                if (result.IsPopup)
                {
                    string actualTypeName = typeName.EndsWith("_Popup", StringComparison.OrdinalIgnoreCase)
                        ? typeName[..^6]
                        : typeName;

                    _popupManager.CreatePopup(actualTypeName, result);
                    continue;
                }

                if (result.View is null)
                {
                    continue;
                }

                _views[name] = result.View;
                _order.Add(name);

                // \brief Visibility hook attach (dedupe)
                result.View.IsVisibleChanged -= OnRootIsVisibleChanged;
                result.View.IsVisibleChanged += OnRootIsVisibleChanged;

                _containerGrid.Children.Add(result.View);

                // \brief Default visibility
                result.View.Visibility = UseSingletonView ? Visibility.Collapsed : Visibility.Visible;
            }

            _viewSwitcher = new ViewSwitcher(_popupManager.Windows, _views, UseSingletonView);

            // \brief Initial switch
            if (!string.IsNullOrWhiteSpace(CurrentView) && _views.ContainsKey(CurrentView))
            {
                SwitchToViewInternal(CurrentView);
            }
            else
            {
                UpdateVisibleView();
            }
        }

        /// <summary>
        /// \brief Build name/type pairs from Items first; fallback to DPs.
        /// </summary>
        private List<(string name, string typeName)> BuildPairsFromItemsOrDps()
        {
            // \brief 1) Items (XAML children)
            var fromItems = BuildPairsFromItems();
            if (fromItems.Count > 0)
            {
                return EnsureUniqueNames(fromItems);
            }

            // \brief 2) DPs (ContentList / ContentTypeList)
            var fromDps = BuildNameTypePairs_NoMutation(ContentList, ContentTypeList);
            return EnsureUniqueNames(fromDps);
        }

        /// <summary>
        /// \brief Build pairs from XAML children Items.
        /// </summary>
        private List<(string name, string typeName)> BuildPairsFromItems()
        {
            var result = new List<(string name, string typeName)>();

            if (Items == null || Items.Count == 0)
            {
                return result;
            }

            foreach (var it in Items)
            {
                var raw = it?.Content?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                // \brief If Popup flag is set, normalize to "*_Popup" convention.
                string typeName = it!.Popup && !raw.EndsWith("_Popup", StringComparison.OrdinalIgnoreCase)
                    ? raw + "_Popup"
                    : raw;

                string name = !string.IsNullOrWhiteSpace(it.Key)
                    ? it.Key
                    : DeriveSimpleKeyFromType(typeName.EndsWith("_Popup", StringComparison.OrdinalIgnoreCase) ? typeName[..^6] : typeName);

                // \brief If a separate Type is provided, use it as concrete type.
                if (!string.IsNullOrWhiteSpace(it.Type))
                {
                    typeName = it.Type;
                    if (it.Popup && !typeName.EndsWith("_Popup", StringComparison.OrdinalIgnoreCase))
                    {
                        typeName += "_Popup";
                    }
                }

                result.Add((name, typeName));
            }

            return result;
        }

        /// <summary>
        /// \brief Gets the declared raw entry at index, from Items first or ContentList as fallback.
        /// </summary>
        private string? GetDeclaredRawEntryAt(int index)
        {
            if (Items != null && Items.Count > 0)
            {
                var it = Items.ElementAtOrDefault(index);
                return it?.Content?.ToString();
            }

            return ContentList?.ElementAtOrDefault(index);
        }

        /// <summary>
        /// \brief Design-time: initialize views.
        /// </summary>
        private void InitDesignViews(List<(string name, string typeName)> pairs)
        {
            foreach (var (name, typeName) in pairs)
            {
                var candidates = BuildDesignTypeCandidates(typeName, DesignBaseNamespaceHint, DesignAssemblyHint);

                UserControl? view = null;
                foreach (var cand in candidates)
                {
                    view = TryCreateDesignView(cand);
                    if (view != null) break;
                }

                view ??= MakeDesignPlaceholder(name);

                if (AutoWireDesignViewModel && view.DataContext == null)
                {
                    AttachDesignViewModelIfEmpty(view);
                }

                _views[name] = view;
                _order.Add(name);
            }

            _containerGrid.Children.Clear();

            var idx = Math.Clamp(SelectView, 0, Math.Max(0, _order.Count - 1));
            if (_order.Count > 0)
            {
                var key = _order[idx];
                _containerGrid.Children.Add(_views[key]);
                _designLastIndex = idx;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>
        /// \brief Returns whether this control is currently in design mode.
        /// </summary>
        private bool IsInDesignMode()
            => DesignerProperties.GetIsInDesignMode(this);

        /// <summary>
        /// \brief Ensure unique logical names.
        /// </summary>
        private static List<(string name, string typeName)> EnsureUniqueNames(List<(string name, string typeName)> pairs)
        {
            var result = new List<(string name, string typeName)>(pairs);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < result.Count; i++)
            {
                var (n, t) = result[i];
                if (string.IsNullOrWhiteSpace(n))
                {
                    n = DeriveSimpleKeyFromType(t);
                }

                if (seen.Add(n))
                {
                    result[i] = (n, t);
                    continue;
                }

                int k = 2;
                string c;
                do
                {
                    c = $"{n}_{k++}";
                }
                while (!seen.Add(c));

                result[i] = (c, t);
            }

            return result;
        }

        /// <summary>
        /// \brief Builds logical name and type name pairs from name/type collections without mutating the DPs.
        /// </summary>
        private static List<(string name, string typeName)> BuildNameTypePairs_NoMutation(
            ObservableCollection<string>? names,
            ObservableCollection<string>? types)
        {
            var result = new List<(string name, string typeName)>();

            var hasNames = names is { Count: > 0 };
            var hasTypes = types is { Count: > 0 };

            if (!hasNames && !hasTypes)
            {
                return result;
            }

            if (hasNames && hasTypes)
            {
                var count = Math.Min(names!.Count, types!.Count);
                for (int i = 0; i < count; i++)
                {
                    var typeName = types[i] ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(names[i])
                        ? DeriveSimpleKeyFromType(typeName)
                        : names[i];

                    result.Add((name, typeName));
                }
            }
            else if (hasTypes)
            {
                foreach (var t in types!)
                {
                    var typeName = t ?? string.Empty;
                    var name = DeriveSimpleKeyFromType(typeName);
                    result.Add((name, typeName));
                }
            }
            else
            {
                foreach (var n in names!)
                {
                    var typeName = n ?? string.Empty;
                    var name = DeriveSimpleKeyFromType(typeName);
                    result.Add((name, typeName));
                }
            }

            return result;
        }

        /// <summary>
        /// \brief Derives a simple key name from a raw type name.
        /// </summary>
        private static string DeriveSimpleKeyFromType(string raw)
            => string.IsNullOrWhiteSpace(raw)
                ? string.Empty
                : (raw.Contains('.') ? raw.Split('.').Last() : raw);

        /// <summary>
        /// \brief Builds candidate type names for design-time resolution.
        /// </summary>
        private static string[] BuildDesignTypeCandidates(string raw, string baseNs, string asmHint)
        {
            var list = new List<string> { raw };

            if (!string.IsNullOrWhiteSpace(asmHint) && !raw.Contains(","))
            {
                list.Add($"{raw}, {asmHint}");
            }

            if (!raw.Contains('.') && !string.IsNullOrWhiteSpace(baseNs))
            {
                var full = $"{baseNs}.{raw}";
                list.Add(full);

                if (!string.IsNullOrWhiteSpace(asmHint))
                {
                    list.Add($"{full}, {asmHint}");
                }
            }

            return list.Distinct().ToArray();
        }

        /// <summary>
        /// \brief Creates a UserControl instance by type name at design-time.
        /// </summary>
        private static UserControl? TryCreateDesignView(string typeName)
        {
            try
            {
                Type? t = null;

                if (typeName.Contains(","))
                {
                    t = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
                }

                if (t == null)
                {
                    t = FindTypeByName(typeName);
                }

                if (t == null || !typeof(UserControl).IsAssignableFrom(t))
                {
                    return null;
                }

                try
                {
                    return Activator.CreateInstance(t) as UserControl;
                }
                catch
                {
                    return Activator.CreateInstance(t, nonPublic: true) as UserControl;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// \brief Searches for a Type across loaded assemblies by full or short name.
        /// </summary>
        private static Type? FindTypeByName(string name)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();

            if (name.Contains('.'))
            {
                foreach (var asm in asms)
                {
                    var t = asm.GetType(name, throwOnError: false, ignoreCase: false);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }

            foreach (var asm in asms)
            {
                try
                {
                    var t = asm.GetTypes().FirstOrDefault(x => x.Name == name);
                    if (t != null)
                    {
                        return t;
                    }
                }
                catch
                {
                    // ignore reflection errors
                }
            }

            return null;
        }

        /// <summary>
        /// \brief Creates a placeholder control when design-time view cannot be instantiated.
        /// </summary>
        private static UserControl MakeDesignPlaceholder(string title)
        {
            return new UserControl
            {
                Content = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.LightSteelBlue,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8),
                    Child = new TextBlock
                    {
                        Text = $"[design] {title}",
                        FontSize = 16,
                        Foreground = System.Windows.Media.Brushes.Black
                    }
                }
            };
        }

        /// <summary>
        /// \brief Attaches a design-time ViewModel if DataContext is empty.
        /// </summary>
        private static void AttachDesignViewModelIfEmpty(UserControl view)
        {
            if (view.DataContext != null)
            {
                return;
            }

            var vmType = ResolveDesignViewModelType(view.GetType());
            if (vmType == null)
            {
                return;
            }

            try
            {
                var vm = DMContainer.Resolve(vmType);
                if (vm != null)
                {
                    view.DataContext = vm;
                }
            }
            catch
            {
                // keep design resilient
            }
        }

        /// <summary>
        /// \brief Resolves a ViewModel type from a View type using common naming conventions.
        /// </summary>
        private static Type? ResolveDesignViewModelType(Type viewType)
        {
            var fullCandidate = viewType.FullName + "ViewModel";
            var nsCandidate = $"{viewType.Namespace}.{viewType.Name}ViewModel";
            var shortCandidate = viewType.Name + "ViewModel";

            var t = FindTypeByName(fullCandidate);
            if (t != null) return t;

            t = FindTypeByName(nsCandidate);
            if (t != null) return t;

            return FindTypeByName(shortCandidate);
        }
    }

    /// <summary>
    /// \class DreamineTabHostItem
    /// \brief Logical item used inside DreamineTabHost to declare a view/popup entry.
    /// Details:
    ///  - Content: default raw type/name string.
    ///  - Key: optional logical key override (if empty, derived from Type/Content).
    ///  - Type: optional concrete type name override (if empty, Content is used).
    ///  - Popup: if true, forces "*_Popup" semantics.
    /// </summary>
    public class DreamineTabHostItem : ContentControl
    {
        /// <summary>
        /// \brief DP backing field for Popup.
        /// </summary>
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.Register(
                nameof(Popup),
                typeof(bool),
                typeof(DreamineTabHostItem),
                new PropertyMetadata(false));

        /// <summary>
        /// \brief Gets or sets whether this item represents a popup view.
        /// </summary>
        public bool Popup
        {
            get => (bool)GetValue(PopupProperty);
            set => SetValue(PopupProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for Key.
        /// </summary>
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register(
                nameof(Key),
                typeof(string),
                typeof(DreamineTabHostItem),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// \brief Optional logical key override.
        /// </summary>
        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        /// <summary>
        /// \brief DP backing field for Type.
        /// </summary>
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(string),
                typeof(DreamineTabHostItem),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// \brief Optional concrete type name override.
        /// </summary>
        public string Type
        {
            get => (string)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }
    }
}
