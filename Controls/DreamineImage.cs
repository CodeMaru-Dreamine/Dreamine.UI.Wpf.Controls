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
	/// @brief VsLibrary 공통 이미지 컨트롤.
	///
	/// @details
	/// - 전역 스타일 자동 병합(DreamineImageStyle.xaml).
	/// - CornerRadius 클리핑 지원.
	/// - FallbackSource(로드 실패 시 대체 이미지) 지원.
	/// - ClickCommand, CommandParameter 지원.
	/// - IsWindowDragEnabled=true 시 제목표시줄처럼 드래그(DragMove) 가능.
	/// - IsDoubleClickToggleMaximize=true 시 더블클릭으로 최대화/복원 토글.
	/// </summary>
	public class DreamineImage : Control
	{

		// Control 클래스에는 이미 BorderThickness, BorderBrush가 있습니다.
		// CornerRadius는 Control에 없으므로 직접 추가합니다.
		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register(
				nameof(CornerRadius),
				typeof(CornerRadius),
				typeof(DreamineImage),
				new FrameworkPropertyMetadata(new CornerRadius(0)));

		// Image의 Source와 같은 역할을 할 프로퍼티를 새로 정의합니다.
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register(
				nameof(Source),
				typeof(ImageSource),
				typeof(DreamineImage),
				new FrameworkPropertyMetadata(null));

		public static readonly DependencyProperty FallbackSourceProperty =
			DependencyProperty.Register(
				nameof(FallbackSource),
				typeof(ImageSource),
				typeof(DreamineImage),
				new FrameworkPropertyMetadata(null));

		public static readonly DependencyProperty ClickCommandProperty =
			DependencyProperty.Register(
				nameof(ClickCommand),
				typeof(ICommand),
				typeof(DreamineImage),
				new PropertyMetadata(null));

		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.Register(
				nameof(CommandParameter),
				typeof(object),
				typeof(DreamineImage),
				new PropertyMetadata(null));

		public static readonly DependencyProperty IsWindowDragEnabledProperty =
			DependencyProperty.Register(
				nameof(IsWindowDragEnabled),
				typeof(bool),
				typeof(DreamineImage),
				new PropertyMetadata(false));

		public static readonly DependencyProperty IsDoubleClickToggleMaximizeProperty =
			DependencyProperty.Register(
				nameof(IsDoubleClickToggleMaximize),
				typeof(bool),
				typeof(DreamineImage),
				new PropertyMetadata(true));

		// Stretch 속성도 Image에서 가져와서 새로 정의합니다.
		public static readonly DependencyProperty StretchProperty =
			DependencyProperty.Register(
				nameof(Stretch),
				typeof(Stretch),
				typeof(DreamineImage),
				new PropertyMetadata(Stretch.Uniform));

		public CornerRadius CornerRadius
		{
			get => (CornerRadius)GetValue(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}

		// 새로 추가한 Source 프로퍼티
		public ImageSource? Source
		{
			get => (ImageSource?)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public ImageSource? FallbackSource
		{
			get => (ImageSource?)GetValue(FallbackSourceProperty);
			set => SetValue(FallbackSourceProperty, value);
		}

		public ICommand? ClickCommand
		{
			get => (ICommand?)GetValue(ClickCommandProperty);
			set => SetValue(ClickCommandProperty, value);
		}

		public object? CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		public bool IsWindowDragEnabled
		{
			get => (bool)GetValue(IsWindowDragEnabledProperty);
			set => SetValue(IsWindowDragEnabledProperty, value);
		}

		public bool IsDoubleClickToggleMaximize
		{
			get => (bool)GetValue(IsDoubleClickToggleMaximizeProperty);
			set => SetValue(IsDoubleClickToggleMaximizeProperty, value);
		}

		public Stretch Stretch
		{
			get => (Stretch)GetValue(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		/// <summary>
		/// @brief 정적 생성자: 스타일 병합 및 컨테이너 바인딩.
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
		/// @brief 생성자: 마우스/로드 이벤트 연결.
		/// </summary>
		public DreamineImage()
		{
			PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
			MouseLeftButtonUp += OnMouseLeftButtonUp;
			MouseMove += OnMouseMove;
			// WPF Image에는 MouseDoubleClick 이벤트가 없으므로 사용하지 않음.
		}

		private Point _pressPoint;
		private bool _isDragging;

		/// <summary>
		/// @brief 마우스 다운: 더블클릭/드래그 시작 판단.
		/// @details
		/// - e.ClickCount == 2 이고 IsDoubleClickToggleMaximize 가 true이면 최대화/복원 토글 후 종료.
		/// - 그렇지 않으면 드래그 기준점 기록 및 캡처.
		/// </summary>
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
		/// @brief 마우스 무브: 드래그 임계값 초과 시 DragMove 실행.
		/// </summary>
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
		/// @brief 마우스 업: 드래그가 아니면 ClickCommand 실행.
		/// </summary>
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
