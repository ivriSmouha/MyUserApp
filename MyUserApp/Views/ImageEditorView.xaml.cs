// File: MyUserApp/Views/ImageEditorView.xaml.cs
using MyUserApp.Models;
using MyUserApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyUserApp.Views
{
    public partial class ImageEditorView : UserControl
    {
        private Point? _startPoint = null;
        private Point _panStartPoint;
        private Ellipse _previewEllipse;
        private bool _isPanning = false;

        public ImageEditorView()
        {
            InitializeComponent();
        }

        private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;

        private void ContentGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;
            Point position = e.GetPosition(ContentGrid);
            ViewModel.SetTransformOrigin(position);
            double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1;
            ViewModel.AdjustZoom(zoomFactor);
        }

        private void ContentGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Space))
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(this);
                ContentGrid.Cursor = Cursors.Hand;
                ContentGrid.CaptureMouse();
                return;
            }
            DrawingCanvas_MouseLeftButtonDown(sender, e);
        }

        private void ContentGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _panStartPoint.X;
                double deltaY = currentPos.Y - _panStartPoint.Y;
                _panStartPoint = currentPos;
                ViewModel?.AdjustPan(deltaX, deltaY);
                return;
            }
            DrawingCanvas_MouseMove(sender, e);
        }

        private void ContentGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                ContentGrid.Cursor = Cursors.Arrow;
                ContentGrid.ReleaseMouseCapture();
                return;
            }
            DrawingCanvas_MouseLeftButtonUp(sender, e);
        }

        private void Annotation_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is AnnotationModel annotation)
            {
                ViewModel?.SelectAnnotation(annotation);
                e.Handled = true;
            }
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.SelectAnnotation(null);
            _startPoint = e.GetPosition(PreviewCanvas); // Use PreviewCanvas

            _previewEllipse = new Ellipse
            {
                Stroke = Brushes.White,
                StrokeDashArray = new DoubleCollection { 2 },
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(_previewEllipse, _startPoint.Value.X);
            Canvas.SetTop(_previewEllipse, _startPoint.Value.Y);
            PreviewCanvas.Children.Add(_previewEllipse);
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_startPoint == null || e.LeftButton != MouseButtonState.Pressed) return;
            Point currentPoint = e.GetPosition(PreviewCanvas); // Use PreviewCanvas

            var x = System.Math.Min(currentPoint.X, _startPoint.Value.X);
            var y = System.Math.Min(currentPoint.Y, _startPoint.Value.Y);
            var width = System.Math.Abs(currentPoint.X - _startPoint.Value.X);
            var height = System.Math.Abs(currentPoint.Y - _startPoint.Value.Y);
            var diameter = System.Math.Max(width, height);

            Canvas.SetLeft(_previewEllipse, x);
            Canvas.SetTop(_previewEllipse, y);
            _previewEllipse.Width = diameter;
            _previewEllipse.Height = diameter;
        }

        private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_startPoint == null) return;
            if (_previewEllipse != null) PreviewCanvas.Children.Remove(_previewEllipse);

            // =============================================
            // ===      THE C# FIX (ALL CALCULATIONS)    ===
            // =============================================
            double canvasWidth = PreviewCanvas.ActualWidth;
            double canvasHeight = PreviewCanvas.ActualHeight;

            if (canvasWidth > 0 && canvasHeight > 0)
            {
                Point endPoint = e.GetPosition(PreviewCanvas);
                double absoluteRadius = System.Math.Max(System.Math.Abs(endPoint.X - _startPoint.Value.X), System.Math.Abs(endPoint.Y - _startPoint.Value.Y)) / 2;
                if (absoluteRadius > 2)
                {
                    double absoluteCenterX = _startPoint.Value.X + (endPoint.X - _startPoint.Value.X) / 2;
                    double absoluteCenterY = _startPoint.Value.Y + (endPoint.Y - _startPoint.Value.Y) / 2;
                    double relativeCenterX = absoluteCenterX / canvasWidth;
                    double relativeCenterY = absoluteCenterY / canvasHeight;
                    double relativeRadius = absoluteRadius / canvasWidth;
                    ViewModel?.CreateAndExecuteAddAnnotation(relativeCenterX, relativeCenterY, relativeRadius);
                }
            }
            _startPoint = null;
        }
    }
}