// File: MyUserApp/Views/ImageEditorView.xaml.cs
using MyUserApp.Models;
using MyUserApp.ViewModels;
using System; // Required for Math
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyUserApp.Views
{
    /// <summary>
    /// Interaction logic for the image editor view.
    /// This version uses direct canvas manipulation for drawing and displaying annotations,
    /// providing a simple, reliable, and performant solution.
    /// </summary>
    public partial class ImageEditorView : UserControl
    {
        // This point now represents the CENTER of the annotation being drawn.
        private Point? _startPoint = null;
        private Point _panStartPoint;
        private Ellipse _previewEllipse;
        private bool _isPanning = false;
        private List<Ellipse> _annotationEllipses = new List<Ellipse>();

        public ImageEditorView()
        {
            InitializeComponent();
            Loaded += ImageEditorView_Loaded;
            // The ViewModel now handles filter changes, but we still need to trigger the update.
            // A cleaner way is to subscribe to the ViewModel's PropertyChanged event.
            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is ImageEditorViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        // If a filter property changes, we must redraw the canvas.
                        if (args.PropertyName == nameof(vm.ShowAiAnnotations) ||
                            args.PropertyName == nameof(vm.ShowInspectorAnnotations) ||
                            args.PropertyName == nameof(vm.ShowVerifierAnnotations))
                        {
                            UpdateAnnotationDisplay();
                        }
                    };
                }
            };
        }

        private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;

        private void ImageEditorView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                // When the collection in the ViewModel changes (e.g., an annotation is added/deleted),
                // we must redraw everything on our canvas.
                ViewModel.CurrentAnnotations.CollectionChanged += (s, args) => UpdateAnnotationDisplay();
                UpdateAnnotationDisplay();
            }
        }

        /// <summary>
        /// This is the master method for drawing all annotations. It clears the canvas
        /// and redraws every annotation from the ViewModel's collection, ensuring the
        /// display is always in sync with the data.
        /// </summary>
        private void UpdateAnnotationDisplay()
        {
            if (ViewModel == null) return;

            // Remove all previously drawn annotation ellipses.
            foreach (var ellipse in _annotationEllipses)
            {
                DrawingCanvas.Children.Remove(ellipse);
            }
            _annotationEllipses.Clear();

            // Get the actual bounds of the image content (accounting for letterboxing).
            var imageBounds = GetActualImageBounds();
            if (imageBounds == Rect.Empty) return;

            // Create and draw a new ellipse for each annotation in the current collection.
            foreach (var annotation in ViewModel.CurrentAnnotations)
            {
                // Check if the annotation should be visible based on the current filter settings.
                if (!ShouldShowAnnotation(annotation)) continue;

                // Convert the annotation's stored relative coordinates (0.0-1.0)
                // into absolute pixel coordinates on the canvas.
                double absoluteCenterX = imageBounds.Left + (annotation.CenterX * imageBounds.Width);
                double absoluteCenterY = imageBounds.Top + (annotation.CenterY * imageBounds.Height);
                double absoluteRadius = annotation.Radius * imageBounds.Width; // Use width for consistent circles.

                var ellipse = new Ellipse
                {
                    Width = absoluteRadius * 2,
                    Height = absoluteRadius * 2,
                    Fill = Brushes.Transparent,
                    StrokeThickness = annotation.IsSelected ? 4 : 2,
                    Stroke = GetAnnotationBrush(annotation),
                    Cursor = Cursors.Hand,
                    Tag = annotation // Store the data object in the Tag for easy retrieval on click.
                };

                // Position the ellipse on the canvas by its top-left corner.
                Canvas.SetLeft(ellipse, absoluteCenterX - absoluteRadius);
                Canvas.SetTop(ellipse, absoluteCenterY - absoluteRadius);

                ellipse.MouseLeftButtonDown += AnnotationEllipse_MouseLeftButtonDown;

                DrawingCanvas.Children.Add(ellipse);
                _annotationEllipses.Add(ellipse);
            }
        }

        private void AnnotationEllipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is AnnotationModel annotation)
            {
                ViewModel?.SelectAnnotation(annotation);
                // After selection changes, IsSelected is updated on the model. We must redraw
                // to reflect the change in stroke thickness or color.
                UpdateAnnotationDisplay();
                e.Handled = true; // Prevent the click from bubbling up to the grid.
            }
        }

        private bool ShouldShowAnnotation(AnnotationModel annotation)
        {
            if (ViewModel == null) return false;
            return annotation.Author switch
            {
                AuthorType.Inspector => ViewModel.ShowInspectorAnnotations,
                AuthorType.Verifier => ViewModel.ShowVerifierAnnotations,
                AuthorType.AI => ViewModel.ShowAiAnnotations,
                _ => true
            };
        }

        private Brush GetAnnotationBrush(AnnotationModel annotation)
        {
            if (annotation.IsSelected) return Brushes.LimeGreen;
            return annotation.Author switch
            {
                AuthorType.Inspector => Brushes.DodgerBlue,
                AuthorType.Verifier => Brushes.Yellow,
                AuthorType.AI => Brushes.Red,
                _ => Brushes.White
            };
        }

        private Rect GetActualImageBounds()
        {
            if (MainImage.Source == null || MainImage.ActualWidth <= 0 || MainImage.ActualHeight <= 0) return Rect.Empty;

            double elementWidth = MainImage.ActualWidth;
            double elementHeight = MainImage.ActualHeight;
            double imageWidth = MainImage.Source.Width;
            double imageHeight = MainImage.Source.Height;
            double elementAspect = elementWidth / elementHeight;
            double imageAspect = imageWidth / imageHeight;

            double contentWidth, contentHeight, offsetX, offsetY;
            if (imageAspect > elementAspect) // Image is wider, letterbox top/bottom
            {
                contentWidth = elementWidth;
                contentHeight = elementWidth / imageAspect;
                offsetX = 0;
                offsetY = (elementHeight - contentHeight) / 2;
            }
            else // Image is taller, letterbox left/right
            {
                contentHeight = elementHeight;
                contentWidth = elementHeight * imageAspect;
                offsetX = (elementWidth - contentWidth) / 2;
                offsetY = 0;
            }
            return new Rect(offsetX, offsetY, contentWidth, contentHeight);
        }

        #region Standard View Interaction (Zoom, Pan)
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
            StartDrawing(e);
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
            UpdateDrawing(e);
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
            FinishDrawing(e);
        }
        #endregion

        #region Intuitive Drawing Logic
        // ===================================================================
        // ==            FIX FOR INTUITIVE DRAWING (Bug #6)                 ==
        // ===================================================================
        // This section implements the new "center-out" drawing logic.

        /// <summary>
        /// Start drawing a new annotation. The first click defines the CENTER.
        /// </summary>
        private void StartDrawing(MouseButtonEventArgs e)
        {
            ViewModel?.SelectAnnotation(null); // Deselect any existing annotation
            _startPoint = e.GetPosition(DrawingCanvas); // Capture the center point

            _previewEllipse = new Ellipse
            {
                Stroke = Brushes.White,
                StrokeDashArray = new DoubleCollection { 2 },
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Colors.White) { Opacity = 0.1 },
                IsHitTestVisible = false // The preview should not interfere with mouse events
            };
            DrawingCanvas.Children.Add(_previewEllipse);
        }

        /// <summary>
        /// Update the drawing preview as the user drags the mouse.
        /// The circle now expands from a fixed center.
        /// </summary>
        private void UpdateDrawing(MouseEventArgs e)
        {
            // Only draw if we are in a drawing operation.
            if (_startPoint == null || e.LeftButton != MouseButtonState.Pressed || _previewEllipse == null) return;

            Point currentPoint = e.GetPosition(DrawingCanvas);
            Point centerPoint = _startPoint.Value;

            // Calculate radius as the direct distance from the center to the mouse pointer.
            double radius = Math.Sqrt(Math.Pow(currentPoint.X - centerPoint.X, 2) + Math.Pow(currentPoint.Y - centerPoint.Y, 2));

            // The diameter is twice the radius.
            double diameter = radius * 2;

            // Position the preview ellipse's top-left corner so its center aligns with _startPoint.
            Canvas.SetLeft(_previewEllipse, centerPoint.X - radius);
            Canvas.SetTop(_previewEllipse, centerPoint.Y - radius);
            _previewEllipse.Width = diameter;
            _previewEllipse.Height = diameter;
        }

        /// <summary>
        /// Finish drawing and create the final annotation in the ViewModel.
        /// </summary>
        private void FinishDrawing(MouseButtonEventArgs e)
        {
            // Ensure we were actually drawing.
            if (_startPoint == null) return;

            // Clean up the preview ellipse from the canvas.
            if (_previewEllipse != null)
            {
                DrawingCanvas.Children.Remove(_previewEllipse);
                _previewEllipse = null;
            }

            // Get image content bounds for coordinate conversion.
            var imageBounds = GetActualImageBounds();
            if (imageBounds == Rect.Empty)
            {
                _startPoint = null;
                return;
            }

            // The center was our start point, the edge is the final mouse position.
            Point centerOnCanvas = _startPoint.Value;
            Point edgeOnCanvas = e.GetPosition(DrawingCanvas);

            // Calculate the final radius in canvas pixels.
            double radiusOnCanvas = Math.Sqrt(Math.Pow(edgeOnCanvas.X - centerOnCanvas.X, 2) + Math.Pow(edgeOnCanvas.Y - centerOnCanvas.Y, 2));

            // Only create an annotation if it's a meaningful size.
            if (radiusOnCanvas > 3)
            {
                // CONVERT canvas coordinates to image-relative coordinates (0.0 - 1.0).
                // This is the crucial step to make annotations resolution-independent.

                // 1. Convert the canvas center point to be relative to the image content area.
                double relativeCenterX = (centerOnCanvas.X - imageBounds.Left) / imageBounds.Width;
                double relativeCenterY = (centerOnCanvas.Y - imageBounds.Top) / imageBounds.Height;

                // 2. Convert the canvas radius to be relative to the image content width.
                //    We consistently use width to ensure circles don't deform on non-square images.
                double relativeRadius = radiusOnCanvas / imageBounds.Width;

                // 3. Clamp values to prevent out-of-bounds data if user drags outside the image.
                relativeCenterX = Math.Max(0, Math.Min(1, relativeCenterX));
                relativeCenterY = Math.Max(0, Math.Min(1, relativeCenterY));

                // 4. Ask the ViewModel to create the annotation with the final relative data.
                ViewModel?.CreateAndExecuteAddAnnotation(relativeCenterX, relativeCenterY, relativeRadius);
            }

            // Reset the drawing state.
            _startPoint = null;
        }
        // ===================================================================

        /// <summary>
        /// This method is a compatibility placeholder and is not used.
        /// The actual click handling is done in AnnotationEllipse_MouseLeftButtonDown.
        /// </summary>
        private void Annotation_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Not used. Handled by direct event subscription on the created ellipses.
        }
    }
    #endregion
}