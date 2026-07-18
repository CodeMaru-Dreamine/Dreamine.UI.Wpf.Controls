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
    /// \if KO
    /// <para>문자열 키로 뷰를 동적 생성·전환하고 팝업 및 디자인 타임 표시를 지원하는 탭 호스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a tab host that dynamically creates and switches views by string key with popup and design-time support.</para>
    /// \endif
    /// \class DreamineTabHost
    /// \brief Content control that dynamically loads and switches views by string keys.
    /// Details:
    ///  - IMPORTANT: XAML children are mapped to <see cref="Items"/> (NOT Content) via <see cref="ContentPropertyAttribute"/>.
    ///  - This enables:
    ///      &lt;DreamineTabHost&gt;
    ///          &lt;DreamineTabHostItem Content="A"/&gt;
    ///          &lt;DreamineTabHostItem Content="B"/&gt;
    ///      &lt;/DreamineTabHost&gt;
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
        /// \if KO
        /// <para>XAML 자식으로 선언된 탭 항목 모음을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the tab-item collection declared through XAML children.</para>
        /// \endif
        /// \brief XAML child items collection.
        /// Details:
        ///  - XAML children &lt;DreamineTabHostItem .../&gt; are added here automatically.
        ///  - This is NOT a DependencyProperty intentionally (to keep parsing simple and stable).
        /// </summary>
        public ObservableCollection<DreamineTabHostItem> Items { get; } = new ObservableCollection<DreamineTabHostItem>();

        /// <summary>
        /// \if KO
        /// <para>마지막으로 활성화된 비팝업 루트 뷰를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the last active non-popup root view.</para>
        /// \endif
        /// </summary>
        private FrameworkElement? _currentView;

        /// <summary>
        /// \if KO
        /// <para>논리 뷰 이름별 런타임 또는 디자인 뷰를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores runtime or design views by logical view name.</para>
        /// \endif
        /// </summary>
        private readonly Dictionary<string, FrameworkElement> _views = new Dictionary<string, FrameworkElement>();

        /// <summary>
        /// \if KO
        /// <para>인덱스 기반 전환에 사용할 논리 키 순서를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the logical-key order used for index-based switching.</para>
        /// \endif
        /// </summary>
        private readonly List<string> _order = new List<string>();

        /// <summary>
        /// \if KO
        /// <para>공유 팝업 창 관리자를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the shared popup-window manager.</para>
        /// \endif
        /// </summary>
        private readonly PopupWindowManager _popupManager = PopupWindowManager.Instance;

        /// <summary>
        /// \if KO
        /// <para>현재 뷰 및 팝업을 전환할 도우미를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the helper that switches current views and popups.</para>
        /// \endif
        /// </summary>
        private ViewSwitcher? _viewSwitcher;

        /// <summary>
        /// \if KO
        /// <para>현재 뷰를 담는 시각적 그리드 호스트를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the visual grid host for current views.</para>
        /// \endif
        /// </summary>
        private Grid _containerGrid = null!;

        /// <summary>
        /// \if KO
        /// <para>디스패처 틱에 뷰 전환이 예약되었는지 나타냅니다.</para>
        /// \endif
        /// \if EN
        /// <para>Indicates whether a view switch is scheduled for a dispatcher tick.</para>
        /// \endif
        /// </summary>
        private bool _isSwitchScheduled;

        /// <summary>
        /// \if KO
        /// <para>디자인 타임에 마지막으로 선택된 인덱스를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the last selected design-time index.</para>
        /// \endif
        /// </summary>
        private int _designLastIndex = -1;

        /// <summary>
        /// \if KO
        /// <para>중복 콜백을 막기 위해 루트 뷰별 마지막 표시 상태를 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Caches the last visibility state per root view to prevent duplicate callbacks.</para>
        /// \endif
        /// </summary>
        private readonly Dictionary<FrameworkElement, bool> _lastVisibilityState = new Dictionary<FrameworkElement, bool>();

        #region ===== Popup awareness: Flag DP & Readonly DP =====

        /// <summary>
        /// \if KO
        /// <para>전환 전에 대상의 팝업 여부를 계산할지 지정하는 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the dependency property controlling pre-switch popup analysis.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty TargetAnalyzedProperty =
            DependencyProperty.Register(
                nameof(TargetAnalyzed),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(false));

        /// <summary>
        /// \if KO
        /// <para>전환 전에 대상의 팝업 여부를 계산하고 노출할지 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets whether popup status is computed and exposed before switching.</para>
        /// \endif
        /// </summary>
        public bool TargetAnalyzed
        {
            get => (bool)GetValue(TargetAnalyzedProperty);
            set => SetValue(TargetAnalyzedProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>현재 대상 팝업 여부의 읽기 전용 종속성 속성 키입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the read-only dependency-property key for current-target popup status.</para>
        /// \endif
        /// </summary>
        private static readonly DependencyPropertyKey IsCurrentTargetPopupPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsCurrentTargetPopup),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(false));

        /// <summary>
        /// \if KO
        /// <para>대기 중인 대상의 팝업 여부를 나타내는 읽기 전용 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the read-only dependency property indicating whether the pending target is a popup.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty IsCurrentTargetPopupProperty =
            IsCurrentTargetPopupPropertyKey.DependencyProperty;

        /// <summary>
        /// \if KO
        /// <para>대기 중인 전환 대상이 팝업인지 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets whether the pending switch target is a popup.</para>
        /// \endif
        /// </summary>
        public bool IsCurrentTargetPopup
        {
            get => (bool)GetValue(IsCurrentTargetPopupProperty);
            private set => SetValue(IsCurrentTargetPopupPropertyKey, value);
        }

        #endregion

        /// <summary>
        /// \if KO
        /// <para>새 탭 호스트를 만들고 항목 모음 변경 시 뷰를 다시 초기화하도록 등록합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new tab host and registers view reinitialization on item changes.</para>
        /// \endif
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
        /// \if KO
        /// <para>논리 뷰 이름 목록 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the logical view-name collection dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty ContentListProperty =
            DependencyProperty.Register(
                nameof(ContentList),
                typeof(ObservableCollection<string>),
                typeof(DreamineTabHost),
                new PropertyMetadata(new ObservableCollection<string>(), OnContentInputsChanged));

        /// <summary>
        /// \if KO
        /// <para>논리 뷰 또는 형식 이름 목록을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the logical view-name or type-name collection.</para>
        /// \endif
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
        /// \if KO
        /// <para>구체적인 뷰 형식 이름 목록 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the concrete view-type-name collection dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty ContentTypeListProperty =
            DependencyProperty.Register(
                nameof(ContentTypeList),
                typeof(ObservableCollection<string>),
                typeof(DreamineTabHost),
                new PropertyMetadata(null, OnContentInputsChanged));

        /// <summary>
        /// \if KO
        /// <para><see cref="ContentList"/>와 인덱스로 대응하는 선택적 구체 뷰 형식 목록을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets optional concrete view types matched by index with <see cref="ContentList"/>.</para>
        /// \endif
        /// </summary>
        public ObservableCollection<string> ContentTypeList
        {
            get => (ObservableCollection<string>)GetValue(ContentTypeListProperty);
            set => SetValue(ContentTypeListProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>현재 논리 뷰 이름 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the current logical-view-name dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty CurrentViewProperty =
            DependencyProperty.Register(
                nameof(CurrentView),
                typeof(string),
                typeof(DreamineTabHost),
                new PropertyMetadata(string.Empty, OnCurrentViewChanged));

        /// <summary>
        /// \if KO
        /// <para>문자열 기반 탐색에 사용할 현재 논리 뷰 이름을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the current logical view name used for string-based navigation.</para>
        /// \endif
        /// </summary>
        public string CurrentView
        {
            get => (string)GetValue(CurrentViewProperty);
            set => SetValue(CurrentViewProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>현재 뷰 인덱스 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the current-view-index dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty SelectViewProperty =
            DependencyProperty.Register(
                nameof(SelectView),
                typeof(int),
                typeof(DreamineTabHost),
                new PropertyMetadata(0, OnSelectViewChanged));

        /// <summary>
        /// \if KO
        /// <para>0부터 시작하는 현재 뷰 인덱스를 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the zero-based current-view index.</para>
        /// \endif
        /// </summary>
        public int SelectView
        {
            get => (int)GetValue(SelectViewProperty);
            set => SetValue(SelectViewProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>뷰 싱글톤 재사용 설정 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the singleton-view reuse dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty UseSingletonViewProperty =
            DependencyProperty.Register(
                nameof(UseSingletonView),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(true, OnContentInputsChanged));

        /// <summary>
        /// \if KO
        /// <para>뷰를 싱글톤으로 재사용할지 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets whether views are reused as singletons.</para>
        /// \endif
        /// </summary>
        public bool UseSingletonView
        {
            get => (bool)GetValue(UseSingletonViewProperty);
            set => SetValue(UseSingletonViewProperty, value);
        }

        // ----- Design hints -----

        /// <summary>
        /// \if KO
        /// <para>디자인 타임 기본 네임스페이스 힌트 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the design-time base-namespace hint dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty DesignBaseNamespaceHintProperty =
            DependencyProperty.Register(
                nameof(DesignBaseNamespaceHint),
                typeof(string),
                typeof(DreamineTabHost),
                new PropertyMetadata(string.Empty, OnDesignHintChanged));

        /// <summary>
        /// \if KO
        /// <para>디자인 타임 짧은 형식 이름 확인용 기본 네임스페이스를 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the base namespace used to resolve short type names at design time.</para>
        /// \endif
        /// </summary>
        public string DesignBaseNamespaceHint
        {
            get => (string)GetValue(DesignBaseNamespaceHintProperty);
            set => SetValue(DesignBaseNamespaceHintProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>디자인 타임 어셈블리 힌트 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the design-time assembly-hint dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty DesignAssemblyHintProperty =
            DependencyProperty.Register(
                nameof(DesignAssemblyHint),
                typeof(string),
                typeof(DreamineTabHost),
                new PropertyMetadata(string.Empty, OnDesignHintChanged));

        /// <summary>
        /// \if KO
        /// <para>디자인 타임 형식 확인용 어셈블리 힌트를 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the assembly hint used for design-time type resolution.</para>
        /// \endif
        /// </summary>
        public string DesignAssemblyHint
        {
            get => (string)GetValue(DesignAssemblyHintProperty);
            set => SetValue(DesignAssemblyHintProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>디자인 뷰 모델 자동 연결 설정 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the automatic design-view-model wiring dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty AutoWireDesignViewModelProperty =
            DependencyProperty.Register(
                nameof(AutoWireDesignViewModel),
                typeof(bool),
                typeof(DreamineTabHost),
                new PropertyMetadata(true, OnDesignHintChanged));

        /// <summary>
        /// \if KO
        /// <para>데이터 컨텍스트가 없을 때 디자인 뷰 모델을 자동 연결할지 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets whether a design-time view model is attached when the data context is null.</para>
        /// \endif
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
        /// \if KO
        /// <para>콘텐츠 입력이 변경되면 호스트 뷰를 다시 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Reinitializes hosted views when content inputs change.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>입력이 변경된 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object whose input changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>속성 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Property-change data.</para>
        /// \endif
        /// </param>
        private static void OnContentInputsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DreamineTabHost c)
            {
                c.TryInitViews();
            }
        }

        /// <summary>
        /// \if KO
        /// <para>디자인 모드에서 힌트가 변경되면 호스트 뷰를 다시 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Reinitializes hosted views when hints change in design mode.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>힌트가 변경된 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object whose hint changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>속성 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Property-change data.</para>
        /// \endif
        /// </param>
        private static void OnDesignHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DreamineTabHost c && c.IsInDesignMode())
            {
                c.TryInitViews();
            }
        }

        /// <summary>
        /// \if KO
        /// <para>현재 뷰 이름 변경을 디스패처에서 비동기 전환으로 예약합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Schedules a current-view-name change as an asynchronous dispatcher switch.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>현재 뷰가 변경된 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object whose current view changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>새 뷰 이름을 포함하는 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Change data containing the new view name.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>선택 인덱스 변경 시 뷰 전환을 예약합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Schedules a view switch when the selected index changes.</para>
        /// \endif
        /// </summary>
        /// <param name="d">
        /// \if KO
        /// <para>선택이 변경된 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The object whose selection changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>선택 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Selection-change data.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>XAML 자식 파싱이 끝난 뒤 컨텍스트 유휴 시점에 뷰 초기화를 예약합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Schedules view initialization at context-idle after XAML child parsing completes.</para>
        /// \endif
        /// </summary>
        /// <param name="e">
        /// \if KO
        /// <para>초기화 이벤트 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initialization event data.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>빠르게 연속된 선택 변경을 하나의 디스패처 전환으로 병합합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Coalesces rapid selection changes into a single dispatcher switch.</para>
        /// \endif
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
        /// \if KO
        /// <para>논리 키로 일반 뷰 또는 팝업을 전환하고 현재 루트 뷰를 갱신합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Switches a regular view or popup by logical key and updates the current root view.</para>
        /// \endif
        /// </summary>
        /// <param name="targetKey">
        /// \if KO
        /// <para>전환할 논리 뷰 키입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The logical view key to switch to.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>런타임에서 선택 인덱스를 안정된 키 순서로 변환해 뷰를 전환합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Converts the selected index to a stable ordered key and switches the runtime view.</para>
        /// \endif
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
        /// \if KO
        /// <para>디자인 타임에 그리드의 단일 자식을 교체해 선택 뷰를 표시합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Displays the selected design-time view by replacing the grid's single child.</para>
        /// \endif
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
        /// \if KO
        /// <para>루트 뷰 표시 상태 변경을 중복 제거하여 <see cref="IVisibilityAware"/> 수명 콜백으로 전달합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Deduplicates root-view visibility changes and forwards them to <see cref="IVisibilityAware"/> lifecycle callbacks.</para>
        /// \endif
        /// </summary>
        /// <param name="sender">
        /// \if KO
        /// <para>표시 상태가 변경된 루트 뷰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The root view whose visibility changed.</para>
        /// \endif
        /// </param>
        /// <param name="e">
        /// \if KO
        /// <para>새 표시 상태를 포함하는 변경 데이터입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Change data containing the new visibility state.</para>
        /// \endif
        /// </param>
        /// <exception cref="InvalidCastException">
        /// \if KO
        /// <para>새 값이 <see cref="bool"/>이 아닐 때 발생할 수 있습니다.</para>
        /// \endif
        /// \if EN
        /// <para>May be thrown when the new value is not a <see cref="bool"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>항목 또는 종속성 속성 입력에서 뷰를 만들고 팝업·표시 수명·전환기를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Materializes views from item or dependency-property inputs and initializes popups, visibility lifecycle, and switching.</para>
        /// \endif
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
        /// \if KO
        /// <para>XAML 항목을 우선 사용하고 없으면 종속성 속성 모음에서 이름·형식 쌍을 만듭니다.</para>
        /// \endif
        /// \if EN
        /// <para>Builds name/type pairs from XAML items first, falling back to dependency-property collections.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>중복 없는 논리 이름과 형식 이름 쌍입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A list of unique logical-name and type-name pairs.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>XAML 자식 항목의 콘텐츠·키·형식·팝업 설정에서 이름·형식 쌍을 만듭니다.</para>
        /// \endif
        /// \if EN
        /// <para>Builds name/type pairs from content, key, type, and popup settings of XAML child items.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>유효한 자식 항목에서 만든 이름·형식 쌍입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Name/type pairs created from valid child items.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>지정 인덱스의 원시 선언을 항목에서 우선 찾고 콘텐츠 목록을 대체 경로로 사용합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the raw declaration at an index from items first, using the content list as fallback.</para>
        /// \endif
        /// </summary>
        /// <param name="index">
        /// \if KO
        /// <para>확인할 0 기반 인덱스입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The zero-based index to inspect.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>원시 선언 문자열이며 없으면 <see langword="null"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The raw declaration string, or <see langword="null"/> if absent.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>디자인 타임 형식 후보를 생성해 뷰 또는 자리표시자를 만들고 선택 항목을 표시합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates design-time views or placeholders from type candidates and displays the selected item.</para>
        /// \endif
        /// </summary>
        /// <param name="pairs">
        /// \if KO
        /// <para>만들 논리 이름과 형식 이름 쌍입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The logical-name and type-name pairs to materialize.</para>
        /// \endif
        /// </param>
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
        /// \if KO
        /// <para>현재 컨트롤이 디자이너에서 실행 중인지 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether the control is currently running in a designer.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>디자인 모드이면 <see langword="true"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para><see langword="true"/> when running in design mode.</para>
        /// \endif
        /// </returns>
        private bool IsInDesignMode()
            => DesignerProperties.GetIsInDesignMode(this);

        /// <summary>
        /// \if KO
        /// <para>빈 논리 이름을 파생하고 중복 이름에 순번 접미사를 붙입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Derives empty logical names and appends numeric suffixes to duplicates.</para>
        /// \endif
        /// </summary>
        /// <param name="pairs">
        /// \if KO
        /// <para>정규화할 이름·형식 쌍입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The name/type pairs to normalize.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>고유 논리 이름을 가진 새 목록입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A new list with unique logical names.</para>
        /// \endif
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para><paramref name="pairs"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="pairs"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>원본 모음을 변경하지 않고 이름 및 형식 모음에서 대응 쌍을 만듭니다.</para>
        /// \endif
        /// \if EN
        /// <para>Builds corresponding name/type pairs without mutating the source collections.</para>
        /// \endif
        /// </summary>
        /// <param name="names">
        /// \if KO
        /// <para>선택적 논리 이름 모음입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The optional logical-name collection.</para>
        /// \endif
        /// </param>
        /// <param name="types">
        /// \if KO
        /// <para>선택적 형식 이름 모음입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The optional type-name collection.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>사용 가능한 입력에서 만든 이름·형식 쌍입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Name/type pairs created from available inputs.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>정규화된 형식 이름의 마지막 구간에서 단순 논리 키를 파생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Derives a simple logical key from the final segment of a qualified type name.</para>
        /// \endif
        /// </summary>
        /// <param name="raw">
        /// \if KO
        /// <para>원시 형식 이름입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The raw type name.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>단순 키이며 입력이 비어 있으면 빈 문자열입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The simple key, or an empty string when the input is blank.</para>
        /// \endif
        /// </returns>
        private static string DeriveSimpleKeyFromType(string raw)
            => string.IsNullOrWhiteSpace(raw)
                ? string.Empty
                : (raw.Contains('.') ? raw.Split('.').Last() : raw);

        /// <summary>
        /// \if KO
        /// <para>원시 이름과 선택적 네임스페이스·어셈블리 힌트로 디자인 타임 형식 후보를 만듭니다.</para>
        /// \endif
        /// \if EN
        /// <para>Builds design-time type candidates from a raw name and optional namespace and assembly hints.</para>
        /// \endif
        /// </summary>
        /// <param name="raw">
        /// \if KO
        /// <para>원시 형식 이름입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The raw type name.</para>
        /// \endif
        /// </param>
        /// <param name="baseNs">
        /// \if KO
        /// <para>기본 네임스페이스 힌트입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The base-namespace hint.</para>
        /// \endif
        /// </param>
        /// <param name="asmHint">
        /// \if KO
        /// <para>어셈블리 힌트입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The assembly hint.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>중복이 제거된 형식 이름 후보 배열입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A deduplicated array of type-name candidates.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>디자인 타임에 형식 이름으로 사용자 컨트롤을 만들며 실패하면 null을 반환합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates a user control by type name at design time, returning null on failure.</para>
        /// \endif
        /// </summary>
        /// <param name="typeName">
        /// \if KO
        /// <para>만들 형식 이름입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The type name to instantiate.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>만든 사용자 컨트롤이며 확인 또는 생성 실패 시 <see langword="null"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The created user control, or <see langword="null"/> when resolution or creation fails.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>로드된 어셈블리에서 정규화된 이름 또는 짧은 이름으로 형식을 찾습니다.</para>
        /// \endif
        /// \if EN
        /// <para>Searches loaded assemblies for a type by qualified or short name.</para>
        /// \endif
        /// </summary>
        /// <param name="name">
        /// \if KO
        /// <para>찾을 형식 이름입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The type name to find.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>찾은 형식이며 없으면 <see langword="null"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The resolved type, or <see langword="null"/> if absent.</para>
        /// \endif
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// \if KO
        /// <para><paramref name="name"/>이 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="name"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>디자인 타임 뷰를 만들 수 없을 때 표시할 자리표시자 컨트롤을 만듭니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates a placeholder control shown when a design-time view cannot be instantiated.</para>
        /// \endif
        /// </summary>
        /// <param name="title">
        /// \if KO
        /// <para>자리표시자에 표시할 제목입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The title displayed by the placeholder.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>디자인 안내 콘텐츠를 포함하는 사용자 컨트롤입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A user control containing design-time informational content.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>데이터 컨텍스트가 비어 있으면 이름 규칙으로 디자인 뷰 모델을 확인하여 연결합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Resolves and attaches a convention-based design view model when the data context is empty.</para>
        /// \endif
        /// </summary>
        /// <param name="view">
        /// \if KO
        /// <para>뷰 모델을 연결할 사용자 컨트롤입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The user control to which the view model is attached.</para>
        /// \endif
        /// </param>
        /// <exception cref="NullReferenceException">
        /// \if KO
        /// <para><paramref name="view"/>가 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="view"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>일반적인 이름 규칙으로 뷰 형식에 대응하는 뷰 모델 형식을 찾습니다.</para>
        /// \endif
        /// \if EN
        /// <para>Resolves the view-model type corresponding to a view type through common naming conventions.</para>
        /// \endif
        /// </summary>
        /// <param name="viewType">
        /// \if KO
        /// <para>대응 뷰 모델을 찾을 뷰 형식입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The view type whose view model is resolved.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>찾은 뷰 모델 형식이며 없으면 <see langword="null"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The resolved view-model type, or <see langword="null"/> if absent.</para>
        /// \endif
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// \if KO
        /// <para><paramref name="viewType"/>이 <see langword="null"/>일 때 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when <paramref name="viewType"/> is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
    /// \if KO
    /// <para>탭 호스트 안에서 일반 뷰 또는 팝업을 선언하는 논리 항목입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a logical item that declares a regular view or popup inside a tab host.</para>
    /// \endif
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
        /// \if KO
        /// <para>팝업 항목 여부 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the popup-item-state dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.Register(
                nameof(Popup),
                typeof(bool),
                typeof(DreamineTabHostItem),
                new PropertyMetadata(false));

        /// <summary>
        /// \if KO
        /// <para>이 항목이 팝업 뷰를 나타내는지 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets whether this item represents a popup view.</para>
        /// \endif
        /// </summary>
        public bool Popup
        {
            get => (bool)GetValue(PopupProperty);
            set => SetValue(PopupProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>선택적 논리 키 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the optional logical-key dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register(
                nameof(Key),
                typeof(string),
                typeof(DreamineTabHostItem),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// \if KO
        /// <para>파생 키 대신 사용할 선택적 논리 키를 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets an optional logical key that overrides the derived key.</para>
        /// \endif
        /// </summary>
        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        /// <summary>
        /// \if KO
        /// <para>선택적 구체 형식 이름 종속성 속성을 식별합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Identifies the optional concrete-type-name dependency property.</para>
        /// \endif
        /// </summary>
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(string),
                typeof(DreamineTabHostItem),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// \if KO
        /// <para>콘텐츠 대신 사용할 선택적 구체 형식 이름을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets an optional concrete type name that overrides the content.</para>
        /// \endif
        /// </summary>
        public string Type
        {
            get => (string)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }
    }
}
