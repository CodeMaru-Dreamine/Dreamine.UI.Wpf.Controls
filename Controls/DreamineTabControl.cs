using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>탭 닫기, 분리 창, 컨텍스트 메뉴와 전용 스타일을 제공하는 사용자 지정 탭 컨트롤입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom tab control with closing, floating-window detachment, context menus, and dedicated styling.</para>
	/// \endif
	/// </summary>
	public class DreamineTabControl : TabControl
	{
		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 탭 컨트롤 테마 리소스를 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges tab-control theme resources.</para>
		/// \endif
		/// </summary>
		static DreamineTabControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineTabControl),
				new FrameworkPropertyMetadata(typeof(DreamineTabControl)));

			var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineTabControlStyle.xaml", UriKind.RelativeOrAbsolute);

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
		/// <para>탭 컨트롤을 만들고 닫기·분리 동작 및 컨텍스트 메뉴 연결을 초기화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes the tab control and its closing, detachment, and context-menu behavior.</para>
		/// \endif
		/// </summary>
		public DreamineTabControl()
		{
			this.PreviewMouseDoubleClick += OnTabItemDoubleClick;

			CloseTabCommand = new RelayCommand<TabItem>(tab =>
			{
				if (tab != null && Items.Contains(tab))
				{
					Items.Remove(tab);
				}
			});

			Loaded += (s, e) =>
			{
				foreach (var tab in Items.OfType<VsTabItem>())
				{
					AttachContextMenu(tab);
				}

				ItemContainerGenerator.ItemsChanged += (s2, e2) =>
				{
					foreach (var item in Items.OfType<VsTabItem>())
					{
						if (item.ContextMenu == null)
							AttachContextMenu(item);
					}
				};
			};
		}

		/// <summary>
		/// \if KO
		/// <para>선택된 탭을 더블클릭하면 별도 부동 창으로 분리합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Detaches the selected tab into a floating window when it is double-clicked.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 탭 컨트롤입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The tab control that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>원본 입력 요소와 클릭 정보를 제공하는 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Event data providing the original input element and click information.</para>
		/// \endif
		/// </param>
		private void OnTabItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.OriginalSource is DependencyObject originalSource)
			{
				var tab = FindParent<VsTabItem>(originalSource);
				if (tab != null && tab.IsSelected)
				{
					DetachTab(tab);
					e.Handled = true;
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>탭에 분리 및 닫기 메뉴 항목을 포함하는 컨텍스트 메뉴를 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attaches a context menu containing detach and close items to a tab.</para>
		/// \endif
		/// </summary>
		/// <param name="tab">
		/// \if KO
		/// <para>메뉴를 연결할 탭입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The tab to receive the menu.</para>
		/// \endif
		/// </param>
		private void AttachContextMenu(VsTabItem tab)
		{
			var menu = new ContextMenu();

			var detach = new MenuItem { Header = "Detach Tab" };
			detach.Click += (s, e) => DetachTab(tab);

			var close = new MenuItem { Header = "CloseAsync Tab" };
			close.Click += (s, e) => RemoveTab(tab);

			menu.Items.Add(detach);
			menu.Items.Add(close);

			tab.ContextMenu = menu;
		}

		/// <summary>
		/// \if KO
		/// <para>컨텍스트 메뉴의 닫기 요청을 처리하고 탭 콘텐츠가 해제 가능하면 해제합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles a context-menu close request and disposes tab content when disposable.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 메뉴 항목입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The menu item that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private void OnCloseTabClick(object sender, RoutedEventArgs e)
		{
			if (TryGetTabItemFromContextMenu(sender, out var tab))
			{
				RemoveTab(tab);

				if (tab.Content is IDisposable disposable)
				{
					disposable.Dispose();
				}

				tab.Content = null;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>탭이 현재 항목 컬렉션에 있으면 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Removes a tab when it belongs to the current items collection.</para>
		/// \endif
		/// </summary>
		/// <param name="tab">
		/// \if KO
		/// <para>제거할 탭입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The tab to remove.</para>
		/// \endif
		/// </param>
		public void RemoveTab(TabItem tab)
		{
			if (tab == null) return;

			if (Items.Contains(tab))
			{
				Items.Remove(tab);
			}
		}

		/// <summary>
		/// \if KO
		/// <para>컨텍스트 메뉴의 분리 요청을 처리합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles a context-menu detach request.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 메뉴 항목입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The menu item that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>라우트 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The routed-event data.</para>
		/// \endif
		/// </param>
		private void OnDetachTabClick(object sender, RoutedEventArgs e)
		{
			if (TryGetTabItemFromContextMenu(sender, out var tab))
			{
				DetachTab(tab);
			}
		}

		/// <summary>
		/// \if KO
		/// <para>컨텍스트 메뉴 이벤트 원본에서 소유 탭을 찾습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Resolves the owning tab from a context-menu event source.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>컨텍스트 메뉴 이벤트 원본입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The context-menu event source.</para>
		/// \endif
		/// </param>
		/// <param name="tab">
		/// \if KO
		/// <para>찾은 탭을 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Receives the resolved tab.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>탭을 찾으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when a tab is found.</para>
		/// \endif
		/// </returns>
		private bool TryGetTabItemFromContextMenu(object sender, out VsTabItem tab)
		{
			tab = null!;

			if (sender is MenuItem menuItem &&
				menuItem.Parent is ContextMenu ctx &&
				ctx.PlacementTarget is DependencyObject target)
			{
				tab = FindParent<VsTabItem>(target);
				return tab != null;
			}

			return false;
		}

		/// <summary>
		/// \if KO
		/// <para>시각적 트리를 올라가 지정한 형식의 첫 부모를 찾습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Walks up the visual tree to find the first parent of the specified type.</para>
		/// \endif
		/// </summary>
		/// <typeparam name="T">
		/// \if KO
		/// <para>찾을 부모 종속성 개체 형식입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The dependency-object parent type to find.</para>
		/// \endif
		/// </typeparam>
		/// <param name="child">
		/// \if KO
		/// <para>탐색을 시작할 자식입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The child at which traversal starts.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>찾은 부모이며 없으면 런타임상 null입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The resolved parent, or runtime null when absent.</para>
		/// \endif
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>탐색 중인 개체가 시각적 트리 개체가 아니면 WPF에서 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown by WPF when a traversed object is not a visual-tree object.</para>
		/// \endif
		/// </exception>
		private T FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			while (child != null)
			{
				if (child is T parent)
					return parent;

				child = VisualTreeHelper.GetParent(child);
			}
			return null!;
		}

		/// <summary>
		/// \if KO
		/// <para>탭 콘텐츠를 별도 부동 창으로 옮기고 창이 닫히면 새 탭으로 복원합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Moves tab content into a floating window and restores it as a new tab when the window closes.</para>
		/// \endif
		/// </summary>
		/// <param name="tab">
		/// \if KO
		/// <para>분리할 탭입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The tab to detach.</para>
		/// \endif
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>부동 창을 현재 애플리케이션 상태에서 표시할 수 없으면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the floating window cannot be shown in the current application state.</para>
		/// \endif
		/// </exception>
		private void DetachTab(VsTabItem tab)
		{
			if (tab?.Content is not UIElement content)
				return;

			var header = tab.Header;
			var icon = tab.Icon;
			var isClosable = tab.IsClosable;

			var floatWindow = new Window
			{
				Title = header?.ToString(),
				Content = content,
				Width = 800,
				Height = 600,
				Icon = icon,
			};

			floatWindow.Closed += (s, e) =>
			{
				// Restore tab when floating window is closed
				AddTab(header?.ToString()!, content, icon, isClosable);
			};

			Items.Remove(tab);
			floatWindow.Show();
		}

		/// <summary>
		/// \if KO
		/// <para>지정한 제목, 콘텐츠, 아이콘과 닫기 설정으로 새 탭을 추가하고 선택합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Adds and selects a new tab with the specified title, content, icon, and closable setting.</para>
		/// \endif
		/// </summary>
		/// <param name="title">
		/// \if KO
		/// <para>탭 헤더 제목입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The tab-header title.</para>
		/// \endif
		/// </param>
		/// <param name="content">
		/// \if KO
		/// <para>탭에 표시할 UI 콘텐츠입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The UI content displayed by the tab.</para>
		/// \endif
		/// </param>
		/// <param name="icon">
		/// \if KO
		/// <para>선택적 탭 아이콘입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional tab icon.</para>
		/// \endif
		/// </param>
		/// <param name="isClosable">
		/// \if KO
		/// <para>사용자가 탭을 닫을 수 있는지 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Indicates whether the user may close the tab.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="content"/>가 null이고 WPF 컬렉션 정책이 이를 허용하지 않으면 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <paramref name="content"/> is null and the WPF collection policy rejects it.</para>
		/// \endif
		/// </exception>
		public void AddTab(string title, UIElement content, ImageSource? icon = null, bool isClosable = true)
		{
			var newTab = new VsTabItem
			{
				Header = title,
				Content = content,
				Icon = icon!,
				IsClosable = isClosable,
			};

			Items.Add(newTab);
			SelectedItem = newTab;
		}

		/// <summary>
		/// \if KO
		/// <para>탭 닫기 명령 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the close-tab command dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CloseTabCommandProperty =
			DependencyProperty.Register(nameof(CloseTabCommand), typeof(ICommand), typeof(DreamineTabControl), new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>탭을 닫는 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the command that closes a tab.</para>
		/// \endif
		/// </summary>
		public ICommand CloseTabCommand
		{
			get => (ICommand)GetValue(CloseTabCommandProperty);
			set => SetValue(CloseTabCommandProperty, value);
		}
	}

	/// <summary>
	/// \if KO
	/// <para>닫기 가능 여부, 아이콘과 헤더 디자인 속성을 추가한 사용자 지정 탭 항목입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom tab item with closability, icon, and header design properties.</para>
	/// \endif
	/// </summary>
	public class VsTabItem : TabItem
	{
		/// <summary>
		/// \if KO
		/// <para>탭 닫기 가능 여부 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the tab-closability dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsClosableProperty =
			DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(VsTabItem), new PropertyMetadata(true));

		/// <summary>
		/// \if KO
		/// <para>탭을 닫을 수 있는지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether the tab can be closed.</para>
		/// \endif
		/// </summary>
		public bool IsClosable
		{
			get => (bool)GetValue(IsClosableProperty);
			set => SetValue(IsClosableProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>탭 아이콘 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the tab-icon dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IconProperty =
			DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(VsTabItem), new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>탭에 표시할 아이콘을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the icon displayed by the tab.</para>
		/// \endif
		/// </summary>
		public ImageSource Icon
		{
			get => (ImageSource)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		#region Design Properties

		/// <summary>
		/// \if KO
		/// <para>기본 탭 헤더 배경 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the default tab-header background dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty TabHeaderBackgroundProperty =
			DependencyProperty.Register(nameof(TabHeaderBackground), typeof(Brush), typeof(VsTabItem), new PropertyMetadata(Brushes.LightGray));

		/// <summary>
		/// \if KO
		/// <para>기본 탭 헤더 배경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the default tab-header background brush.</para>
		/// \endif
		/// </summary>
		public Brush TabHeaderBackground
		{
			get => (Brush)GetValue(TabHeaderBackgroundProperty);
			set => SetValue(TabHeaderBackgroundProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>선택된 탭 헤더 배경 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the selected tab-header background dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty TabHeaderSelectedBackgroundProperty =
			DependencyProperty.Register(nameof(TabHeaderSelectedBackground), typeof(Brush), typeof(VsTabItem), new PropertyMetadata(Brushes.White));

		/// <summary>
		/// \if KO
		/// <para>선택된 탭 헤더 배경 브러시를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the selected tab-header background brush.</para>
		/// \endif
		/// </summary>
		public Brush TabHeaderSelectedBackground
		{
			get => (Brush)GetValue(TabHeaderSelectedBackgroundProperty);
			set => SetValue(TabHeaderSelectedBackgroundProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>탭 모서리 반지름 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the tab corner-radius dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty TabCornerRadiusProperty =
			DependencyProperty.Register(nameof(TabCornerRadius), typeof(CornerRadius), typeof(VsTabItem), new PropertyMetadata(new CornerRadius(4)));

		/// <summary>
		/// \if KO
		/// <para>탭 모서리 반지름을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the tab corner radius.</para>
		/// \endif
		/// </summary>
		public CornerRadius TabCornerRadius
		{
			get => (CornerRadius)GetValue(TabCornerRadiusProperty);
			set => SetValue(TabCornerRadiusProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>탭 테두리 두께 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the tab border-thickness dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty TabBorderThicknessProperty =
			DependencyProperty.Register(nameof(TabBorderThickness), typeof(Thickness), typeof(VsTabItem), new PropertyMetadata(new Thickness(1)));

		/// <summary>
		/// \if KO
		/// <para>탭 테두리 두께를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the tab border thickness.</para>
		/// \endif
		/// </summary>
		public Thickness TabBorderThickness
		{
			get => (Thickness)GetValue(TabBorderThicknessProperty);
			set => SetValue(TabBorderThicknessProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>탭 헤더 글꼴 크기 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the tab-header font-size dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty TabHeaderFontSizeProperty =
			DependencyProperty.Register(nameof(TabHeaderFontSize), typeof(double), typeof(VsTabItem), new PropertyMetadata(12.0));

		/// <summary>
		/// \if KO
		/// <para>탭 헤더 글꼴 크기를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the tab-header font size.</para>
		/// \endif
		/// </summary>
		public double TabHeaderFontSize
		{
			get => (double)GetValue(TabHeaderFontSizeProperty);
			set => SetValue(TabHeaderFontSizeProperty, value);
		}

		#endregion

		/// <summary>
		/// \if KO
		/// <para>사용자 지정 탭 항목의 기본 스타일 키를 재정의합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key for the custom tab item.</para>
		/// \endif
		/// </summary>
		static VsTabItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(VsTabItem),
				new FrameworkPropertyMetadata(typeof(VsTabItem)));
		}
	}
}
