// File: MyUserApp/Views/ImageEditorView.xaml.cs
using MyUserApp.Models;
using MyUserApp.ViewModels;
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
    /// Simple, direct solution for image annotation that actually works.
    /// No complex converters - just direct canvas positioning.
    /// </summary>
    public partial class ImageEditorView : UserControl
    {
        private Point? _startPoint = null;
        private Point _panStartPoint;
        private Ellipse _previewEllipse;
        private bool _isPanning = false;
        private List<Ellipse> _annotationEllipses = new List<Ellipse>();

        /// <summary>
        /// Initialize the view and set up event handlers.
        /// </summary>
        public ImageEditorView()
        {
            InitializeComponent();
            Loaded += ImageEditorView_Loaded;
        }

        /// <summary>
        /// Gets the ViewModel for this view.
        /// </summary>
        private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;

        /// <summary>
        /// Set up event handlers when the view is loaded.
        /// </summary>
        private void ImageEditorView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Listen for changes in the annotations collection
                ViewModel.CurrentAnnotations.CollectionChanged += CurrentAnnotations_CollectionChanged;
                UpdateAnnotationDisplay();
            }
        }

        /// <summary>
        /// Update the visual display when annotations change.
        /// </summary>
        private void CurrentAnnotations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateAnnotationDisplay();
        }

        /// <summary>
        /// Recreate all annotation ellipses on the canvas with direct positioning.
        /// This bypasses complex converters and puts annotations exactly where they should be.
        /// </summary>
        private void UpdateAnnotationDisplay()
        {
            // Remove all existing annotation ellipses
            foreach (var ellipse in _annotationEllipses)
            {
                DrawingCanvas.Children.Remove(ellipse);
            }
            _annotationEllipses.Clear();

            // Get current image dimensions and bounds
            if (MainImage.ActualWidth <= 0 || MainImage.ActualHeight <= 0) return;

            var imageBounds = GetActualImageBounds();
            if (imageBounds == Rect.Empty) return;

            System.Diagnostics.Debug.WriteLine($"\n=== DISPLAY UPDATE ===");
            System.Diagnostics.Debug.WriteLine($"MainImage size: {MainImage.ActualWidth} x {MainImage.ActualHeight}");
            System.Diagnostics.Debug.WriteLine($"Image bounds: {imageBounds}");

            // Create new ellipses for each annotation
            int annotationIndex = 0;
            foreach (var annotation in ViewModel.CurrentAnnotations)
            {
                // Skip if filtered out
                if (!ShouldShowAnnotation(annotation)) continue;

                System.Diagnostics.Debug.WriteLine($"\nDisplay Annotation #{annotationIndex}:");
                System.Diagnostics.Debug.WriteLine($"  Stored relative coords: ({annotation.CenterX:F4}, {annotation.CenterY:F4}), Radius: {annotation.Radius:F4}");

                // Calculate absolute position from relative coordinates
                double absoluteCenterX = imageBounds.Left + (annotation.CenterX * imageBounds.Width);
                double absoluteCenterY = imageBounds.Top + (annotation.CenterY * imageBounds.Height);
                double absoluteRadius = annotation.Radius * imageBounds.Width;

                System.Diagnostics.Debug.WriteLine($"  Display absolute coords: Center=({absoluteCenterX:F2}, {absoluteCenterY:F2}), Radius={absoluteRadius:F2}");
                System.Diagnostics.Debug.WriteLine($"  Display circle bounds: Left={absoluteCenterX - absoluteRadius:F2}, Top={absoluteCenterY - absoluteRadius:F2}");

                // Create the ellipse
                var ellipse = new Ellipse
                {
                    Width = absoluteRadius * 2,
                    Height = absoluteRadius * 2,
                    Fill = Brushes.Transparent,
                    StrokeThickness = annotation.IsSelected ? 4 : 2,
                    Stroke = GetAnnotationBrush(annotation),
                    Cursor = Cursors.Hand
                };

                // Position the ellipse
                Canvas.SetLeft(ellipse, absoluteCenterX - absoluteRadius);
                Canvas.SetTop(ellipse, absoluteCenterY - absoluteRadius);

                // Set up click handling
                ellipse.Tag = annotation;
                ellipse.MouseLeftButtonDown += AnnotationEllipse_MouseLeftButtonDown;

                // Add to canvas and track it
                DrawingCanvas.Children.Add(ellipse);
                _annotationEllipses.Add(ellipse);

                annotationIndex++;
            }
        }

        /// <summary>
        /// Handle clicking on annotation ellipses for selection.
        /// </summary>
        private void AnnotationEllipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is AnnotationModel annotation)
            {
                ViewModel?.SelectAnnotation(annotation);
                UpdateAnnotationDisplay(); // Refresh to show selection
                e.Handled = true;
            }
        }

        /// <summary>
        /// Check if an annotation should be shown based on current filters.
        /// </summary>
        private bool ShouldShowAnnotation(AnnotationModel annotation)
        {
            return annotation.Author switch
            {
                AuthorType.Inspector => ViewModel.ShowInspectorAnnotations,
                AuthorType.Verifier => ViewModel.ShowVerifierAnnotations,
                AuthorType.AI => ViewModel.ShowAiAnnotations,
                _ => true
            };
        }

        /// <summary>
        /// Get the brush color for an annotation based on its author and selection state.
        /// </summary>
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

        /// <summary>
        /// Calculate the actual bounds of the image content within the Image element.
        /// Accounts for letterboxing when using Stretch="Uniform".
        /// </summary>
        private Rect GetActualImageBounds()
        {
            if (MainImage.Source == null) return Rect.Empty;

            double elementWidth = MainImage.ActualWidth;
            double elementHeight = MainImage.ActualHeight;
            double imageWidth = MainImage.Source.Width;
            double imageHeight = MainImage.Source.Height;

            if (elementWidth <= 0 || elementHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
                return Rect.Empty;

            double elementAspect = elementWidth / elementHeight;
            double imageAspect = imageWidth / imageHeight;

            double contentWidth, contentHeight, offsetX, offsetY;

            if (imageAspect > elementAspect)
            {
                // Image is wider - fit to width
                contentWidth = elementWidth;
                contentHeight = elementWidth / imageAspect;
                offsetX = 0;
                offsetY = (elementHeight - contentHeight) / 2;
            }
            else
            {
                // Image is taller - fit to height
                contentHeight = elementHeight;
                contentWidth = elementHeight * imageAspect;
                offsetX = (elementWidth - contentWidth) / 2;
                offsetY = 0;
            }

            return new Rect(offsetX, offsetY, contentWidth, contentHeight);
        }

        /// <summary>
        /// Handle mouse wheel for zooming.
        /// </summary>
        private void ContentGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;
            Point position = e.GetPosition(ContentGrid);
            ViewModel.SetTransformOrigin(position);
            double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1;
            ViewModel.AdjustZoom(zoomFactor);
        }

        /// <summary>
        /// Handle mouse down for panning or drawing.
        /// </summary>
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

        /// <summary>
        /// Handle mouse movement for panning or drawing.
        /// </summary>
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

        /// <summary>
        /// Handle mouse up for ending pan or drawing.
        /// </summary>
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

        /// <summary>
        /// Start drawing a new annotation.
        /// </summary>
        private void StartDrawing(MouseButtonEventArgs e)
        {
            ViewModel?.SelectAnnotation(null);
            _startPoint = e.GetPosition(DrawingCanvas);

            _previewEllipse = new Ellipse
            {
                Stroke = Brushes.White,
                StrokeDashArray = new DoubleCollection { 2 },
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Colors.White) { Opacity = 0.1 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(_previewEllipse, _startPoint.Value.X);
            Canvas.SetTop(_previewEllipse, _startPoint.Value.Y);
            DrawingCanvas.Children.Add(_previewEllipse);
        }

        /// <summary>
        /// Update the drawing preview as user drags.
        /// </summary>
        private void UpdateDrawing(MouseEventArgs e)
        {
            if (_startPoint == null || e.LeftButton != MouseButtonState.Pressed || _previewEllipse == null)
                return;

            Point currentPoint = e.GetPosition(DrawingCanvas);

            double x = System.Math.Min(currentPoint.X, _startPoint.Value.X);
            double y = System.Math.Min(currentPoint.Y, _startPoint.Value.Y);
            double width = System.Math.Abs(currentPoint.X - _startPoint.Value.X);
            double height = System.Math.Abs(currentPoint.Y - _startPoint.Value.Y);
            double diameter = System.Math.Max(width, height);

            Canvas.SetLeft(_previewEllipse, x);
            Canvas.SetTop(_previewEllipse, y);
            _previewEllipse.Width = diameter;
            _previewEllipse.Height = diameter;
        }

        /// <summary>
        /// Finish drawing and create the annotation.
        /// </summary>
        private void FinishDrawing(MouseButtonEventArgs e)
        {
            if (_startPoint == null) return;

            if (_previewEllipse != null)
            {
                DrawingCanvas.Children.Remove(_previewEllipse);
                _previewEllipse = null;
            }

            var imageBounds = GetActualImageBounds();
            if (imageBounds == Rect.Empty)
            {
                _startPoint = null;
                return;
            }

            Point endPoint = e.GetPosition(DrawingCanvas);

            // Convert canvas coordinates to image-relative coordinates
            double startX = _startPoint.Value.X - imageBounds.Left;
            double startY = _startPoint.Value.Y - imageBounds.Top;
            double endX = endPoint.X - imageBounds.Left;
            double endY = endPoint.Y - imageBounds.Top;

            // Clamp to image bounds
            startX = System.Math.Max(0, System.Math.Min(startX, imageBounds.Width));
            startY = System.Math.Max(0, System.Math.Min(startY, imageBounds.Height));
            endX = System.Math.Max(0, System.Math.Min(endX, imageBounds.Width));
            endY = System.Math.Max(0, System.Math.Min(endY, imageBounds.Height));

            double absoluteRadius = System.Math.Max(System.Math.Abs(endX - startX), System.Math.Abs(endY - startY)) / 2;

            if (absoluteRadius > 3)
            {
                double absoluteCenterX = startX + (endX - startX) / 2;
                double absoluteCenterY = startY + (endY - startY) / 2;

                // Convert to relative coordinates
                double relativeCenterX = absoluteCenterX / imageBounds.Width;
                double relativeCenterY = absoluteCenterY / imageBounds.Height;
                double relativeRadius = absoluteRadius / imageBounds.Width;

                ViewModel?.CreateAndExecuteAddAnnotation(relativeCenterX, relativeCenterY, relativeRadius);
            }

            _startPoint = null;
        }

        /// <summary>
        /// Handle clicking on annotations (placeholder for XAML compatibility).
        /// </summary>
        private void Annotation_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This method exists for XAML compatibility but is not used
            // The actual handling is done in AnnotationEllipse_MouseLeftButtonDown
        }
    }
}