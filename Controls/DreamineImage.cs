using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Dreamine.MVVM.Core;

namespace Dreamine.UI.Wpf.Controls
{
	/// <summary>
	/// \if KO
	/// <para>둥근 모서리, 대체 이미지, 클릭 명령과 창 드래그 동작을 제공하는 사용자 지정 이미지 컨트롤입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides a custom image control with rounded corners, fallback source, click command, and window-drag behavior.</para>
	/// \endif
	/// </summary>
	public class DreamineImage : Control
	{

		// Control 클래스에는 이미 BorderThickness, BorderBrush가 있습니다.
		// CornerRadius는 Control에 없으므로 직접 추가합니다.
		/// <summary>
		/// \if KO
		/// <para>이미지 모서리 반지름 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the image corner-radius dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register(
				nameof(CornerRadius),
				typeof(CornerRadius),
				typeof(DreamineImage),
				new FrameworkPropertyMetadata(new CornerRadius(0)));

		// Image의 Source와 같은 역할을 할 프로퍼티를 새로 정의합니다.
		/// <summary>
		/// \if KO
		/// <para>기본 이미지 소스 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the primary image-source dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register(
				nameof(Source),
				typeof(ImageSource),
				typeof(DreamineImage),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>기본 이미지 로드 실패 시 사용할 대체 소스 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the fallback-source dependency property used when the primary image fails.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty FallbackSourceProperty =
			DependencyProperty.Register(
				nameof(FallbackSource),
				typeof(ImageSource),
				typeof(DreamineImage),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>클릭 시 실행할 명령 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the command dependency property executed on click.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty ClickCommandProperty =
			DependencyProperty.Register(
				nameof(ClickCommand),
				typeof(ICommand),
				typeof(DreamineImage),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>클릭 명령 매개변수 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the click-command parameter dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.Register(
				nameof(CommandParameter),
				typeof(object),
				typeof(DreamineImage),
				new PropertyMetadata(null));

		/// <summary>
		/// \if KO
		/// <para>창 드래그 활성화 여부 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the dependency property that enables window dragging.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsWindowDragEnabledProperty =
			DependencyProperty.Register(
				nameof(IsWindowDragEnabled),
				typeof(bool),
				typeof(DreamineImage),
				new PropertyMetadata(false));

		/// <summary>
		/// \if KO
		/// <para>더블클릭 최대화 전환 활성화 여부 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the dependency property that enables double-click maximize toggling.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty IsDoubleClickToggleMaximizeProperty =
			DependencyProperty.Register(
				nameof(IsDoubleClickToggleMaximize),
				typeof(bool),
				typeof(DreamineImage),
				new PropertyMetadata(true));

		// Stretch 속성도 Image에서 가져와서 새로 정의합니다.
		/// <summary>
		/// \if KO
		/// <para>이미지 늘이기 방식 종속성 속성입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Identifies the image-stretch dependency property.</para>
		/// \endif
		/// </summary>
		public static readonly DependencyProperty StretchProperty =
			DependencyProperty.Register(
				nameof(Stretch),
				typeof(Stretch),
				typeof(DreamineImage),
				new PropertyMetadata(Stretch.Uniform));

		/// <summary>
		/// \if KO
		/// <para>이미지 모서리 반지름을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the image corner radius.</para>
		/// \endif
		/// </summary>
		public CornerRadius CornerRadius
		{
			get => (CornerRadius)GetValue(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}

		// 새로 추가한 Source 프로퍼티
		/// <summary>
		/// \if KO
		/// <para>표시할 기본 이미지 소스를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the primary image source to display.</para>
		/// \endif
		/// </summary>
		public ImageSource? Source
		{
			get => (ImageSource?)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>기본 소스를 표시할 수 없을 때 사용할 대체 이미지 소스를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the fallback image source used when the primary source cannot be displayed.</para>
		/// \endif
		/// </summary>
		public ImageSource? FallbackSource
		{
			get => (ImageSource?)GetValue(FallbackSourceProperty);
			set => SetValue(FallbackSourceProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>드래그가 아닌 클릭에서 실행할 명령을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the command executed for a nondrag click.</para>
		/// \endif
		/// </summary>
		public ICommand? ClickCommand
		{
			get => (ICommand?)GetValue(ClickCommandProperty);
			set => SetValue(ClickCommandProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>클릭 명령에 전달할 매개변수를 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the parameter passed to the click command.</para>
		/// \endif
		/// </summary>
		public object? CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>마우스 드래그로 상위 창을 이동할지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether mouse dragging moves the containing window.</para>
		/// \endif
		/// </summary>
		public bool IsWindowDragEnabled
		{
			get => (bool)GetValue(IsWindowDragEnabledProperty);
			set => SetValue(IsWindowDragEnabledProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>더블클릭으로 상위 창의 최대화와 복원을 전환할지 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets whether double-click toggles maximized and normal states of the containing window.</para>
		/// \endif
		/// </summary>
		public bool IsDoubleClickToggleMaximize
		{
			get => (bool)GetValue(IsDoubleClickToggleMaximizeProperty);
			set => SetValue(IsDoubleClickToggleMaximizeProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>이미지가 사용 가능한 영역을 채우는 방식을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets how the image fills its available area.</para>
		/// \endif
		/// </summary>
		public Stretch Stretch
		{
			get => (Stretch)GetValue(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		/// <summary>
		/// \if KO
		/// <para>기본 스타일 키를 재정의하고 이미지 테마 리소스를 병합합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Overrides the default style key and merges the image theme resources.</para>
		/// \endif
		/// </summary>
		static DreamineImage()
		{
			try
			{

				DefaultStyleKeyProperty.OverrideMetadata(typeof(DreamineImage),
					new FrameworkPropertyMetadata(typeof(DreamineImage)));

				var uri = new Uri("/Dreamine.UI.Wpf.Themes;component/DreamineImageStyle.xaml", UriKind.RelativeOrAbsolute);
				bool alreadyAdded = Application.Current.Resources.MergedDictionaries
					.OfType<ResourceDictionary>()
					.Any(x => x.Source != null && x.Source.Equals(uri));

				if (!alreadyAdded)
				{
					var dict = new ResourceDictionary { Source = uri };
					Application.Current.Resources.MergedDictionaries.Add(dict);
				}

			}
			catch
			{
				// 디자이너/테스트 환경 보호
			}
		}

		/// <summary>
		/// \if KO
		/// <para>이미지 컨트롤을 만들고 마우스 누르기, 놓기와 이동 이벤트를 연결합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes the image control and attaches mouse-down, mouse-up, and mouse-move events.</para>
		/// \endif
		/// </summary>
		public DreamineImage()
		{
			PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
			MouseLeftButtonUp += OnMouseLeftButtonUp;
			MouseMove += OnMouseMove;
			// WPF Image에는 MouseDoubleClick 이벤트가 없으므로 사용하지 않음.
		}

		/// <summary>
		/// \if KO
		/// <para>마우스를 누른 기준 위치입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the mouse-press reference position.</para>
		/// \endif
		/// </summary>
		private Point _pressPoint;
		/// <summary>
		/// \if KO
		/// <para>현재 입력이 드래그로 판정되었는지 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Indicates whether the current input has been classified as a drag.</para>
		/// \endif
		/// </summary>
		private bool _isDragging;

		/// <summary>
		/// \if KO
		/// <para>더블클릭이면 창 상태를 전환하고, 그 외에는 드래그 기준점을 기록해 마우스를 캡처합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Toggles window state on double-click; otherwise, records the drag origin and captures the mouse.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 컨트롤입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The control that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>클릭 횟수와 위치를 제공하는 마우스 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Mouse event data providing click count and position.</para>
		/// \endif
		/// </param>
		private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// 1) 더블클릭 토글 (WPF는 ClickCount로 판별)
			if (IsDoubleClickToggleMaximize && e.ClickCount == 2)
			{
				var win = Window.GetWindow(this);
				if (win != null)
				{
					try
					{
						win.WindowState = win.WindowState == WindowState.Maximized
							? WindowState.Normal
							: WindowState.Maximized;
						e.Handled = true;
						return;
					}
					catch { /* 무시 */ }
				}
			}

			// 2) 드래그 대비 기준점 기록
			_pressPoint = e.GetPosition(this);
			_isDragging = false;
			CaptureMouse();
		}

		/// <summary>
		/// \if KO
		/// <para>마우스 이동이 시스템 드래그 임계값을 넘으면 상위 창의 드래그 이동을 시작합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Starts dragging the containing window when mouse movement exceeds system drag thresholds.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 컨트롤입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The control that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>버튼 상태와 위치를 제공하는 마우스 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Mouse event data providing button state and position.</para>
		/// \endif
		/// </param>
		/// <remarks>
		/// \if KO
		/// <para>마우스 캡처와 DragMove 예외는 UI 입력 흐름을 유지하기 위해 내부에서 무시됩니다.</para>
		/// \endif
		/// \if EN
		/// <para>Mouse-capture and DragMove exceptions are swallowed to preserve the UI input flow.</para>
		/// \endif
		/// </remarks>
		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!IsWindowDragEnabled || e.LeftButton != MouseButtonState.Pressed)
				return;

			var p = e.GetPosition(this);
			var dx = Math.Abs(p.X - _pressPoint.X);
			var dy = Math.Abs(p.Y - _pressPoint.Y);

			if (!_isDragging &&
				(dx > SystemParameters.MinimumHorizontalDragDistance ||
				 dy > SystemParameters.MinimumVerticalDragDistance))
			{
				_isDragging = true;

				// \note DragMove 직전 캡처 해제 (드래그 씹힘 방지 핵심)
				try
				{
					if (Mouse.Captured == this)
						ReleaseMouseCapture();
					else
						Mouse.Capture(null);
				}
				catch { /* ignore */ }

				try
				{
					var win = Window.GetWindow(this);
					win?.DragMove();
				}
				catch
				{
					// DragMove 중 예외는 조용히 무시
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>마우스 캡처를 해제하고 드래그가 아니면 클릭 명령을 실행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Releases mouse capture and executes the click command when the interaction was not a drag.</para>
		/// \endif
		/// </summary>
		/// <param name="sender">
		/// \if KO
		/// <para>이벤트를 발생시킨 컨트롤입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The control that raised the event.</para>
		/// \endif
		/// </param>
		/// <param name="e">
		/// \if KO
		/// <para>마우스 버튼 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The mouse-button event data.</para>
		/// \endif
		/// </param>
		/// <remarks>
		/// \if KO
		/// <para>명령 실행 예외는 현재 구현에서 무시됩니다.</para>
		/// \endif
		/// \if EN
		/// <para>Command-execution exceptions are swallowed by the current implementation.</para>
		/// \endif
		/// </remarks>
		private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			ReleaseMouseCapture();

			if (_isDragging) return; // 드래그로 끝난 경우 클릭 아님

			if (ClickCommand?.CanExecute(CommandParameter) == true)
			{
				try { ClickCommand.Execute(CommandParameter); } catch { /* 무시 */ }
			}
		}

	}
}
