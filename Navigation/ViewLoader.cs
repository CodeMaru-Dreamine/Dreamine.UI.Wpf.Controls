using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.UI.Wpf.Controls.ViewRegion
{
	/// <summary>
	/// @brief View 타입 이름으로 View를 만들고, 대응 ViewModel을 DI로 resolve하여 DataContext에 연결합니다.
	/// @details
	/// - UserControl/Page/Window/그 외 FrameworkElement 모두 지원
	/// - Page는 Frame에 호스팅되며, Navigated/Loaded 타이밍에 맞춰 DataContext를 주입합니다.
	/// - 반환: 임베드 가능한 <see cref="FrameworkElement"/>와 호환성을 위한 <see cref="UserControl"/> 래퍼 동시 제공
	/// </summary>
	public static class ViewLoader
	{
		/// <summary>
		/// @brief 로드된 View 및 ViewModel 메타 정보.
		/// </summary>
		public class LoadedViewInfo
		{
			/// <summary>
			/// @brief 호환성을 위한 UserControl 래퍼. 임베드가 필요한 경우 이 컨트롤을 사용하세요.
			/// @details 원 뷰가 UserControl이면 그대로, 아니면 내부에 래핑된 요소가 들어갑니다.
			/// </summary>
			public UserControl? View { get; set; }

			/// <summary>
			/// @brief 원본/최종 임베드 가능한 뷰 요소(프레임/컨텐츠호스트 포함).
			/// </summary>
			public FrameworkElement? FrameworkView { get; set; }

			/// <summary>
			/// @brief Resolve된 ViewModel 타입(없으면 null).
			/// </summary>
			public Type? ViewModelType { get; set; }

			/// <summary>
			/// @brief _Popup 네이밍 규칙에 의한 팝업 플래그.
			/// </summary>
			public bool IsPopup { get; set; }

			/// <summary>
			/// @brief 싱글톤이 아닐 때 고유 키(멀티 인스턴스 구별용).
			/// </summary>
			public string? UniqueKey { get; set; }
		}

		/// <summary>
		/// @brief View 타입명을 기반으로 View를 생성하고 ViewModel을 바인딩합니다.
		/// @details
		///  - FIX:
		///    - View 생성은 반드시 <see cref="ResolveFrameworkElement"/> 를 통해 수행합니다.
		///      (Page를 UserControl/ContentControl에 직접 넣으면 WPF 규칙 위반으로 예외 발생)
		///    - ViewModel이 없어도 View는 생성하여 화면이 표시되도록 합니다.
		///    - Page는 Frame으로 감싸서 반환되므로, 어디에 embed 하더라도 안정적으로 동작합니다.
		/// </details>
		/// @Param typeName View의 풀네임(끝이 "_Popup"이면 팝업 플래그 처리)
		/// @Param useSingletonView ViewModel을 싱글톤으로 resolve할지 여부
		/// @returns 뷰 인스턴스/뷰모델 메타 정보
		/// </summary>
		/// <summary>
		/// \brief Loads a view and wires its ViewModel by naming conventions.
		/// \param typeName View type name (short or full). Can end with "_Popup".
		/// \param useSingletonView True to reuse singleton View/ViewModel.
		/// \return LoadedViewInfo.
		/// </summary>
		public static LoadedViewInfo LoadViewWithViewModel(string typeName, bool useSingletonView)
		{
			// \brief Parse popup suffix
			bool isPopup = typeName.EndsWith("_Popup", StringComparison.OrdinalIgnoreCase);
			string actualTypeName = isPopup ? typeName[..^6] : typeName;

			// \brief 1) Resolve View type
			var viewType =
				Type.GetType(actualTypeName, throwOnError: false, ignoreCase: true)
				?? AppDomain.CurrentDomain.GetAssemblies()
					.SelectManySafe(a => a.GetTypesSafe())
					.FirstOrDefault(t =>
						t.FullName?.Equals(actualTypeName, StringComparison.OrdinalIgnoreCase) == true ||
						t.Name.Equals(actualTypeName.Split('.').Last(), StringComparison.OrdinalIgnoreCase));

			// \brief 2) Resolve ViewModel type (FIXED)
			Type? viewModelType = ResolveViewModelType_Strict(actualTypeName, viewType);

			// \brief ViewModel exists => reset DataContext strategy may differ
			bool resetDataContext = viewModelType != null;

			object? vmInstance = null;
			string? uniqueKey = null;

			// \brief 3) Create/Resolve ViewModel instance
			if (viewModelType != null)
			{
				if (useSingletonView)
				{
					vmInstance = DMContainer.Resolve(viewModelType) ?? Activator.CreateInstance(viewModelType);
				}
				else
				{
					// \brief Multi instance unique key
					if (!ViewModelKeyCache.IndexMap.ContainsKey(typeName))
					{
						ViewModelKeyCache.IndexMap[typeName] = 0;
					}

					int typeIndex = ViewModelKeyCache.IndexMap[typeName]++;
					uniqueKey = $"{typeName}_{typeIndex:D2}";

					vmInstance = Activator.CreateInstance(viewModelType);
				}
			}

			// \brief 4) Create FrameworkElement
			FrameworkElement framework = ResolveFrameworkElement(viewType, actualTypeName, resetDataContext);

			// \brief 5) Apply DataContext when VM exists
			if (vmInstance != null)
			{
				ApplyDataContext(framework, vmInstance);
			}

			// \brief 6) Ensure wrapper as UserControl
			UserControl wrapper;
			if (framework is UserControl uc)
			{
				wrapper = uc;
			}
			else
			{
				wrapper = new EmbeddedHostControl(framework);

				// \note Ensure wrapper has same DataContext as the hosted framework
				if (wrapper.DataContext == null)
				{
					wrapper.DataContext = framework.DataContext;
				}
			}

			return new LoadedViewInfo
			{
				View = wrapper,
				FrameworkView = framework,
				ViewModelType = viewModelType,
				IsPopup = isPopup,
				UniqueKey = uniqueKey
			};
		}

		/// <summary>
		/// \brief Resolves a ViewModel type with strict rules to avoid selecting View types as ViewModel.
		/// \details
		///  - Candidates ALWAYS end with "ViewModel".
		///  - Candidates are filtered to types assignable to ViewModelBase (or at least not FrameworkElement).
		/// \param actualTypeName Actual view type name without "_Popup".
		/// \param viewType Resolved view type (can be null).
		/// \return ViewModel type or null.
		/// </summary>
		private static Type? ResolveViewModelType_Strict(string actualTypeName, Type? viewType)
		{
			// \brief Candidate full names
			var candidates = new List<string>();

			// \brief Common: same namespace + ViewModel suffix
			candidates.Add(actualTypeName + "ViewModel");

			// \brief Pages -> ViewModels convention
			if (actualTypeName.Contains(".Pages.", StringComparison.OrdinalIgnoreCase))
			{
				candidates.Add(actualTypeName.Replace(".Pages.", ".ViewModels.", StringComparison.OrdinalIgnoreCase) + "ViewModel");
			}

			// \brief Views -> ViewModels convention (optional, but common)
			if (actualTypeName.Contains(".Views.", StringComparison.OrdinalIgnoreCase))
			{
				candidates.Add(actualTypeName.Replace(".Views.", ".ViewModels.", StringComparison.OrdinalIgnoreCase) + "ViewModel");
			}

			// \brief If viewType is known, add "Namespace.ViewModels.{ViewName}ViewModel"
			if (viewType != null)
			{
				string viewNs = viewType.Namespace ?? string.Empty;
				string viewName = viewType.Name;
				if (!string.IsNullOrWhiteSpace(viewNs))
				{
					candidates.Add($"{viewNs}.ViewModels.{viewName}ViewModel");
					candidates.Add($"{viewNs}.{viewName}ViewModel");
				}

				// \brief Short name candidate
				candidates.Add(viewName + "ViewModel");
			}

			// \brief De-duplicate
			candidates = candidates
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			// \brief Search with strict filtering
			foreach (var cand in candidates)
			{
				var t = AppDomain.CurrentDomain.GetAssemblies()
					.SelectManySafe(a => a.GetTypesSafe())
					.FirstOrDefault(x =>
						x.FullName?.Equals(cand, StringComparison.OrdinalIgnoreCase) == true ||
						x.Name.Equals(cand.Split('.').Last(), StringComparison.OrdinalIgnoreCase));

				if (t == null)
				{
					continue;
				}

				// \brief Must be a concrete class
				if (!t.IsClass || t.IsAbstract)
				{
					continue;
				}

				// \brief Prevent selecting View type as ViewModel
				if (typeof(FrameworkElement).IsAssignableFrom(t))
				{
					continue;
				}

				// \brief Strong filter: must be ViewModelBase (your framework)
				if (!typeof(ViewModelBase).IsAssignableFrom(t))
				{
					continue;
				}

				return t;
			}

			return null;
		}

		/// <summary>
		/// @brief 주어진 타입으로부터 임베드 가능한 FrameworkElement를 만듭니다.
		/// @details
		/// - UserControl → 그대로
		/// - Page → Frame에 호스팅(Navigation UI 숨김, History 독립)
		/// - Window → Content 떼서 ContentControl로 래핑
		/// - FrameworkElement → 그대로
		/// - null/실패 → 간단한 안내 UserControl 반환
		/// - <paramref name="resetDataContext"/> 가 true일 때만 DataContext를 초기화합니다.
		/// </details>
		/// <Param name="viewType">생성할 View 타입.</Param>
		/// <Param name="typeName">디버깅용 타입 이름.</Param>
		/// <Param name="resetDataContext">
		///  - true  : Region/ViewLoader가 ViewModel을 책임질 때, 기존 DataContext를 모두 제거합니다.<br/>
		///  - false : 기존 DataContext(부모 상속, AutoWireDesignViewModel 등)를 유지합니다.
		/// </Param>
		/// <returns>임베드 가능한 <see cref="FrameworkElement"/> 인스턴스.</returns>
		private static FrameworkElement ResolveFrameworkElement(Type? viewType, string typeName, bool resetDataContext) 
		{
			if (viewType == null || viewType.IsAbstract)
			{
				return new TextBlock
				{
					Text = $"[No View Type: {typeName}]",
					Margin = new Thickness(8)
				};
			}

			object? instance = null;

			// 1차: VsControls를 통한 생성 시도 (항상 새 인스턴스)
			try
			{
				instance = DMContainer.Resolve(viewType);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[ViewLoader] VsContainer.Resolve 실패: {typeName} - {ex.Message}");
			}

			// 2차: 컨테이너 생성 실패 시 Activator.CreateInstance로 폴백
			if (instance == null)
			{
				try
				{
					instance = Activator.CreateInstance(viewType);
				}
				catch (Exception ex)
				{
					return new TextBlock
					{
						Text = $"[CreateInstance Failed: {typeName}] {ex.Message}",
						Margin = new Thickness(8)
					};
				}
			}

			switch (instance)
			{
				// --- UserControl 그대로 사용 ---
				case UserControl uc:
					{
						if (resetDataContext)
						{
							uc.DataContext = null;
						}
						return uc;
					}

				// --- Page → Frame 호스팅 ---
				case Page page:
					{
						if (resetDataContext)
						{
							page.DataContext = null;
						}

						var frame = new Frame
						{
							Content = page,
							NavigationUIVisibility = NavigationUIVisibility.Hidden,
							JournalOwnership = JournalOwnership.OwnsJournal
						};

						if (resetDataContext)
						{
							frame.DataContext = null;
						}

						return frame;
					}

				// --- Window → ContentControl 호스팅 ---
				case Window win:
					{
						if (win.Content is FrameworkElement content)
						{
							if (resetDataContext)
							{
								content.DataContext = null;
								win.DataContext = null;
							}

							win.Content = null;
							var host = new ContentControl
							{
								Content = content
							};

							if (resetDataContext)
							{
								host.DataContext = null;
							}

							MergeResources(host.Resources, win.Resources);
							return host;
						}

						return new TextBlock
						{
							Text = $"[Window has no Content: {typeName}]",
							Margin = new Thickness(8)
						};
					}

				// --- 기타 FrameworkElement ---
				case FrameworkElement fe:
					{
						if (resetDataContext)
						{
							fe.DataContext = null;
						}
						return fe;
					}

				default:
					{
						return new TextBlock
						{
							Text = $"[Unknown View Type: {viewType.FullName}]",
							Margin = new Thickness(8)
						};
					}
			}
		}

		/// <summary>
		/// @brief 프레임/호스트 내부 실제 컨텐츠에 ViewModel(DataContext)을 적용합니다.
		/// @details
		///   - 이 메서드는 ViewModel 타입이 정상적으로 resolve 된 경우에만 호출됩니다.
		///   - 기존 DataContext 유무와 관계없이 지정된 ViewModel로 덮어씁니다.
		///   - Frame은 Navigation 컨테이너 특성상 즉시 Content가 없을 수 있으므로
		///     Navigated/Loaded 타이밍을 모두 커버합니다.
		/// </details>
		/// <Param name="root">DataContext를 주입할 루트 요소.</Param>
		/// <Param name="vm">ViewModel 인스턴스.</Param>
		private static void ApplyDataContext(FrameworkElement root, object vm)
		{
			if (root == null || vm == null)
			{
				return;
			}

			// --- Frame 전용 처리: Page 로드 타이밍 보장 ---
			if (root is Frame fr)
			{
				// Frame 자신에도 ViewModel 설정
				fr.DataContext = vm;

				// 로컬: 실제 컨텐츠에 주입
				void TrySet(FrameworkElement fe)
				{
					if (fe != null)
					{
						fe.DataContext = vm;
					}
				}

				// 로컬: Loaded 이후 1회성 주입
				void OnLoaded(object? s, RoutedEventArgs e)
				{
					if (s is FrameworkElement fe)
					{
						fe.Loaded -= OnLoaded;
						TrySet(fe);
					}
				}

				// 로컬: 네비게이션 직후 1회성 주입
				void OnNavigated(object? s, NavigationEventArgs e)
				{
					fr.Navigated -= OnNavigated;

					if (fr.Content is FrameworkElement fe)
					{
						if (!fe.IsLoaded)
						{
							fe.Loaded += OnLoaded;
						}

						TrySet(fe);
					}
				}

				// 현재 시점에 컨텐츠가 있으면 즉시/Loaded에서 주입
				if (fr.Content is FrameworkElement current)
				{
					if (!current.IsLoaded)
					{
						current.Loaded += OnLoaded;
					}

					TrySet(current);
				}
				else
				{
					// 아직 Page가 없음 → 다음 네비게이션에서 주입
					fr.Navigated += OnNavigated;
				}

				return;
			}

			// --- ContentControl 호스트 처리 ---
			if (root is ContentControl cc && cc.Content is FrameworkElement innerCc)
			{
				cc.DataContext = vm;
				innerCc.DataContext = vm;
				return;
			}

			// --- 일반 FrameworkElement 처리 ---
			root.DataContext = vm;
		}


		/// <summary>
		/// @brief 리소스 병합(윈도우 → 호스트).
		/// </summary>
		private static void MergeResources(ResourceDictionary target, ResourceDictionary source)
		{
			foreach (var rd in source.MergedDictionaries)
				target.MergedDictionaries.Add(rd);

			foreach (var key in source.Keys)
				if (!target.Contains(key))
					target.Add(key, source[key]);
		}

		/// <summary>
		/// @brief 안전한 타입 열거(ReflectionTypeLoadException 보호).
		/// </summary>
		private static Type[] GetTypesSafe(this Assembly asm)
		{
			try { return asm.GetTypes(); }
			catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).ToArray()!; }
		}

		/// <summary>
		/// @brief 안전한 SelectMany(동적 어셈블리 스킵).
		/// </summary>
		private static IEnumerable<T> SelectManySafe<T>(this IEnumerable<Assembly> assemblies, Func<Assembly, IEnumerable<T>> selector)
		{
			foreach (var a in assemblies)
			{
				if (a.IsDynamic) continue;
				IEnumerable<T>? res = null;
				try { res = selector(a); }
				catch (Exception ex) { Debug.WriteLine($"[ViewLoader] Assembly scan 실패 ({a.FullName}): {ex.Message}"); }
				if (res != null) foreach (var x in res) yield return x;
			}
		}

		/// <summary>
		/// @brief FrameworkElement를 감싸 임베드 가능한 UserControl로 제공하는 래퍼.
		/// </summary>
		private sealed class EmbeddedHostControl : UserControl
		{
			/// <summary>
			/// @Param inner 임베드할 요소
			/// </summary>
			public EmbeddedHostControl(FrameworkElement inner)
			{
				Content = inner;
			}
		}
	}
}
