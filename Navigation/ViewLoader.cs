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
	/// \if KO
	/// <para>이름 규칙과 DI를 이용해 뷰 및 뷰 모델을 만들고 임베드 가능한 요소로 연결합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Creates a view and view model through naming conventions and DI, then wires them into an embeddable element.</para>
	/// \endif
	/// </summary>
	public static class ViewLoader
	{
		/// <summary>
		/// \if KO
		/// <para>로드된 뷰, 뷰 모델 형식 및 인스턴스 구분 정보를 담습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Contains the loaded view, view-model type, and instance-identification metadata.</para>
		/// \endif
		/// </summary>
		public class LoadedViewInfo
		{
			/// <summary>
			/// \if KO
			/// <para>호환성을 위한 사용자 컨트롤 래퍼를 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the user-control wrapper provided for compatibility.</para>
			/// \endif
			/// </summary>
			public UserControl? View { get; set; }

			/// <summary>
			/// \if KO
			/// <para>프레임이나 콘텐츠 호스트를 포함한 최종 임베드 가능 요소를 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the final embeddable element, including any frame or content host.</para>
			/// \endif
			/// </summary>
			public FrameworkElement? FrameworkView { get; set; }

			/// <summary>
			/// \if KO
			/// <para>확인된 뷰 모델 형식을 가져오거나 설정하며 없으면 <see langword="null"/>입니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the resolved view-model type, or <see langword="null"/> when none was found.</para>
			/// \endif
			/// </summary>
			public Type? ViewModelType { get; set; }

			/// <summary>
			/// \if KO
			/// <para><c>_Popup</c> 접미사로 판정한 팝업 여부를 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets whether the <c>_Popup</c> suffix identified the view as a popup.</para>
			/// \endif
			/// </summary>
			public bool IsPopup { get; set; }

			/// <summary>
			/// \if KO
			/// <para>비싱글톤 뷰 모델 인스턴스를 구분하는 고유 키를 가져오거나 설정합니다.</para>
			/// \endif
			/// \if EN
			/// <para>Gets or sets the unique key that distinguishes a non-singleton view-model instance.</para>
			/// \endif
			/// </summary>
			public string? UniqueKey { get; set; }
		}

		/// <summary>
		/// \if KO
		/// <para>형식 이름으로 뷰를 만들고 규칙에 맞는 뷰 모델을 연결하여 로드 결과를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates a view by type name, wires a convention-matched view model, and returns the load result.</para>
		/// \endif
		/// </summary>
		/// <param name="typeName">
		/// \if KO
		/// <para>짧거나 정규화된 뷰 형식 이름이며 <c>_Popup</c>으로 끝날 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>The short or qualified view type name, optionally ending in <c>_Popup</c>.</para>
		/// \endif
		/// </param>
		/// <param name="useSingletonView">
		/// \if KO
		/// <para>컨테이너에서 싱글톤 뷰 모델을 확인하려면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> to resolve a singleton view model from the container.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>임베드 가능한 뷰와 뷰 모델 메타데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The embeddable view and view-model metadata.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="typeName"/>이 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="typeName"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		/// <exception cref="MissingMethodException">
		/// \if KO
		/// <para>비싱글톤 뷰 모델에 매개변수 없는 생성자가 없을 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when a non-singleton view model has no parameterless constructor.</para>
		/// \endif
		/// </exception>
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
					long typeIndex = ViewModelKeyCache.GetOrIncrement(typeName);
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
		/// \if KO
		/// <para>이름 끝의 <c>View</c> 접미사를 대소문자 구분 없이 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Removes a trailing <c>View</c> suffix without case sensitivity.</para>
		/// \endif
		/// </summary>
		/// <param name="name">
		/// \if KO
		/// <para>검사할 형식 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The type name to inspect.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>접미사가 제거된 이름이며 접미사가 없으면 원래 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The name without the suffix, or the original name when no suffix exists.</para>
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
		private static string StripViewSuffix(string name)
		{
			const string suffix = "View";
			if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				return name[..^suffix.Length];
			return name;
		}

		/// <summary>
		/// \if KO
		/// <para>네임스페이스 및 접미사 후보 중 구체적인 <see cref="ViewModelBase"/> 파생 형식을 찾습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Finds a concrete <see cref="ViewModelBase"/> subtype among namespace and suffix candidates.</para>
		/// \endif
		/// </summary>
		/// <param name="actualTypeName">
		/// \if KO
		/// <para><c>_Popup</c> 접미사를 제거한 실제 뷰 형식 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The actual view type name without the <c>_Popup</c> suffix.</para>
		/// \endif
		/// </param>
		/// <param name="viewType">
		/// \if KO
		/// <para>확인된 뷰 형식이며 찾지 못했으면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The resolved view type, or <see langword="null"/> when unavailable.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>규칙과 기본 형식 조건을 만족하는 뷰 모델 형식이며 없으면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A view-model type satisfying naming and base-type rules, or <see langword="null"/> if none exists.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="actualTypeName"/>이 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="actualTypeName"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static Type? ResolveViewModelType_Strict(string actualTypeName, Type? viewType)
		{
			// \brief Candidate full names
			var candidates = new List<string>();

			// \brief Common: same namespace + ViewModel suffix
			candidates.Add(actualTypeName + "ViewModel");
			candidates.Add(StripViewSuffix(actualTypeName) + "ViewModel");

			// \brief Pages -> ViewModels convention
			if (actualTypeName.Contains(".Pages.", StringComparison.OrdinalIgnoreCase))
			{
				var pagesToVm = actualTypeName.Replace(".Pages.", ".ViewModels.", StringComparison.OrdinalIgnoreCase);
				candidates.Add(pagesToVm + "ViewModel");
				candidates.Add(StripViewSuffix(pagesToVm) + "ViewModel");
			}

			// \brief Views -> ViewModels convention (optional, but common)
			if (actualTypeName.Contains(".Views.", StringComparison.OrdinalIgnoreCase))
			{
				var viewsToVm = actualTypeName.Replace(".Views.", ".ViewModels.", StringComparison.OrdinalIgnoreCase);
				candidates.Add(viewsToVm + "ViewModel");
				candidates.Add(StripViewSuffix(viewsToVm) + "ViewModel");
			}

			// \brief If viewType is known, add "Namespace.ViewModels.{ViewName}ViewModel"
			if (viewType != null)
			{
				string viewNs = viewType.Namespace ?? string.Empty;
				string viewName = viewType.Name;
				string viewNameNoSuffix = StripViewSuffix(viewName);
				if (!string.IsNullOrWhiteSpace(viewNs))
				{
					candidates.Add($"{viewNs}.ViewModels.{viewName}ViewModel");
					candidates.Add($"{viewNs}.ViewModels.{viewNameNoSuffix}ViewModel");
					candidates.Add($"{viewNs}.{viewName}ViewModel");
					candidates.Add($"{viewNs}.{viewNameNoSuffix}ViewModel");

					// \brief Sibling "Views" -> "ViewModels" namespace swap
					if (viewNs.Contains(".Views", StringComparison.OrdinalIgnoreCase))
					{
						string swappedNs = viewNs.Replace(".Views", ".ViewModels", StringComparison.OrdinalIgnoreCase);
						candidates.Add($"{swappedNs}.{viewName}ViewModel");
						candidates.Add($"{swappedNs}.{viewNameNoSuffix}ViewModel");
					}
				}

				// \brief Short name candidates
				candidates.Add(viewName + "ViewModel");
				candidates.Add(viewNameNoSuffix + "ViewModel");
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
		/// \if KO
		/// <para>뷰 형식 인스턴스를 만들고 WPF 형식별 규칙에 따라 임베드 가능한 요소로 변환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates a view-type instance and converts it to an embeddable element according to WPF type rules.</para>
		/// \endif
		/// </summary>
		/// <param name="viewType">
		/// \if KO
		/// <para>만들 뷰 형식이며 없으면 안내 요소를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view type to create; an informational element is returned when it is absent.</para>
		/// \endif
		/// </param>
		/// <param name="typeName">
		/// \if KO
		/// <para>오류 안내에 사용할 뷰 형식 이름입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view type name used in diagnostic messages.</para>
		/// \endif
		/// </param>
		/// <param name="resetDataContext">
		/// \if KO
		/// <para>기존 데이터 컨텍스트를 제거하려면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> to clear existing data contexts.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>사용자 컨트롤, 프레임, 콘텐츠 호스트 또는 안내 텍스트로 표현된 임베드 가능 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>An embeddable user control, frame, content host, or informational text element.</para>
		/// \endif
		/// </returns>
		/// <remarks>
		/// \if KO
		/// <para>생성 실패는 안내용 <see cref="TextBlock"/>으로 변환되며 호출자에게 예외를 전파하지 않습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creation failures are converted to an informational <see cref="TextBlock"/> rather than propagated.</para>
		/// \endif
		/// </remarks>
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
		/// \if KO
		/// <para>루트와 실제 호스트 콘텐츠에 뷰 모델을 데이터 컨텍스트로 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies the view model as the data context of both the root and its hosted content.</para>
		/// \endif
		/// </summary>
		/// <param name="root">
		/// \if KO
		/// <para>데이터 컨텍스트를 받을 루트 요소입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The root element that receives the data context.</para>
		/// \endif
		/// </param>
		/// <param name="vm">
		/// \if KO
		/// <para>적용할 뷰 모델 인스턴스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The view-model instance to apply.</para>
		/// \endif
		/// </param>
		/// <remarks>
		/// \if KO
		/// <para>프레임 콘텐츠가 아직 없으면 다음 탐색 또는 로드 시 한 번 적용하며, null 입력은 무시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>If frame content is not yet available, assignment occurs once on the next navigation or load; null inputs are ignored.</para>
		/// \endif
		/// </remarks>
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
#pragma warning disable CS1587 // Doxygen documents local functions; the C# compiler does not attach XML docs to them.
/// \cond LOCAL_FUNCTION_DOCUMENTATION
				/// <summary>
				/// \if KO
				/// <para>지정한 프레임 콘텐츠 요소에 현재 뷰 모델을 즉시 적용합니다.</para>
				/// \endif
				/// \if EN
				/// <para>Immediately applies the current view model to the specified frame-content element.</para>
				/// \endif
				/// </summary>
				/// <param name="fe">
				/// \if KO
				/// <para>데이터 컨텍스트를 설정할 프레임 콘텐츠 요소입니다.</para>
				/// \endif
				/// \if EN
				/// <para>The frame-content element whose data context is set.</para>
				/// \endif
				/// </param>
/// \endcond
				void TrySet(FrameworkElement fe)
				{
					if (fe != null)
					{
						fe.DataContext = vm;
					}
				}
#pragma warning restore CS1587

				// 로컬: Loaded 이후 1회성 주입
#pragma warning disable CS1587 // Doxygen documents local functions; the C# compiler does not attach XML docs to them.
/// \cond LOCAL_FUNCTION_DOCUMENTATION
				/// <summary>
				/// \if KO
				/// <para>프레임 콘텐츠가 로드되면 이벤트 구독을 해제하고 뷰 모델을 한 번 적용합니다.</para>
				/// \endif
				/// \if EN
				/// <para>Detaches the event and applies the view model once frame content is loaded.</para>
				/// \endif
				/// </summary>
				/// <param name="s">
				/// \if KO
				/// <para>로드된 프레임 콘텐츠 요소입니다.</para>
				/// \endif
				/// \if EN
				/// <para>The loaded frame-content element.</para>
				/// \endif
				/// </param>
				/// <param name="e">
				/// \if KO
				/// <para>로드 이벤트 데이터이며 이 처리기에서는 사용하지 않습니다.</para>
				/// \endif
				/// \if EN
				/// <para>The load event data, which this handler does not use.</para>
				/// \endif
				/// </param>
/// \endcond
				void OnLoaded(object? s, RoutedEventArgs e)
				{
					if (s is FrameworkElement fe)
					{
						fe.Loaded -= OnLoaded;
						TrySet(fe);
					}
				}
#pragma warning restore CS1587

				// 로컬: 네비게이션 직후 1회성 주입
#pragma warning disable CS1587 // Doxygen documents local functions; the C# compiler does not attach XML docs to them.
/// \cond LOCAL_FUNCTION_DOCUMENTATION
				/// <summary>
				/// \if KO
				/// <para>프레임 탐색이 완료되면 새 콘텐츠에 뷰 모델을 적용하고 일회성 탐색 구독을 해제합니다.</para>
				/// \endif
				/// \if EN
				/// <para>Applies the view model to newly navigated frame content and removes the one-shot navigation subscription.</para>
				/// \endif
				/// </summary>
				/// <param name="s">
				/// \if KO
				/// <para>탐색 이벤트를 발생시킨 프레임이며 이 처리기에서는 캡처된 프레임을 사용합니다.</para>
				/// \endif
				/// \if EN
				/// <para>The frame that raised the event; this handler uses the captured frame instance.</para>
				/// \endif
				/// </param>
				/// <param name="e">
				/// \if KO
				/// <para>탐색 이벤트 데이터이며 이 처리기에서는 사용하지 않습니다.</para>
				/// \endif
				/// \if EN
				/// <para>The navigation event data, which this handler does not use.</para>
				/// \endif
				/// </param>
/// \endcond
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
#pragma warning restore CS1587

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
		/// \if KO
		/// <para>원본 사전의 병합 사전과 중복되지 않는 리소스를 대상에 추가합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Adds merged dictionaries and nonduplicate resources from a source dictionary to a target.</para>
		/// \endif
		/// </summary>
		/// <param name="target">
		/// \if KO
		/// <para>리소스를 받을 대상 사전입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The target dictionary that receives resources.</para>
		/// \endif
		/// </param>
		/// <param name="source">
		/// \if KO
		/// <para>복사할 원본 사전입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The source dictionary to copy.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="target"/> 또는 <paramref name="source"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="target"/> or <paramref name="source"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		/// <exception cref="ArgumentException">
		/// \if KO
		/// <para>대상 사전에 추가할 수 없는 키가 있을 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when a key cannot be added to the target dictionary.</para>
		/// \endif
		/// </exception>
		private static void MergeResources(ResourceDictionary target, ResourceDictionary source)
		{
			foreach (var rd in source.MergedDictionaries)
				target.MergedDictionaries.Add(rd);

			foreach (var key in source.Keys)
				if (!target.Contains(key))
					target.Add(key, source[key]);
		}

		/// <summary>
		/// \if KO
		/// <para>로드 가능한 형식만 반환하여 부분적인 어셈블리 형식 로드 실패를 허용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Returns loadable types while tolerating partial assembly type-load failures.</para>
		/// \endif
		/// </summary>
		/// <param name="asm">
		/// \if KO
		/// <para>형식을 열거할 어셈블리입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The assembly whose types are enumerated.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>어셈블리에서 성공적으로 로드된 형식 배열입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The array of types successfully loaded from the assembly.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="asm"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="asm"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		private static Type[] GetTypesSafe(this Assembly asm)
		{
			try { return asm.GetTypes(); }
			catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).ToArray()!; }
		}

		/// <summary>
		/// \if KO
		/// <para>동적 어셈블리를 건너뛰고 선택자 실패를 기록하면서 결과를 평탄화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Flattens selector results while skipping dynamic assemblies and logging selector failures.</para>
		/// \endif
		/// </summary>
		/// <typeparam name="T">
		/// \if KO
		/// <para>선택자가 반환하는 요소 형식입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The element type returned by the selector.</para>
		/// \endif
		/// </typeparam>
		/// <param name="assemblies">
		/// \if KO
		/// <para>검사할 어셈블리 시퀀스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The assembly sequence to inspect.</para>
		/// \endif
		/// </param>
		/// <param name="selector">
		/// \if KO
		/// <para>각 어셈블리에서 요소를 선택할 함수입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The function that selects elements from each assembly.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>성공한 선택 결과의 지연 시퀀스입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A deferred sequence of successful selector results.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="assemblies"/> 또는 열거된 항목이 <see langword="null"/>일 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <paramref name="assemblies"/> or an enumerated item is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
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
		/// \if KO
		/// <para>임의의 프레임워크 요소를 사용자 컨트롤 콘텐츠로 감싸는 내부 호스트입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Provides an internal host that wraps any framework element as user-control content.</para>
		/// \endif
		/// </summary>
		private sealed class EmbeddedHostControl : UserControl
		{
			/// <summary>
			/// \if KO
			/// <para>지정한 요소를 콘텐츠로 사용하는 새 호스트를 만듭니다.</para>
			/// \endif
			/// \if EN
			/// <para>Initializes a new host whose content is the specified element.</para>
			/// \endif
			/// </summary>
			/// <param name="inner">
			/// \if KO
			/// <para>임베드할 프레임워크 요소입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The framework element to embed.</para>
			/// \endif
			/// </param>
			public EmbeddedHostControl(FrameworkElement inner)
			{
				Content = inner;
			}
		}
	}
}
