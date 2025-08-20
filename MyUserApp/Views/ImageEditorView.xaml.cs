// File: MyUserApp/Views/ImageEditorView.xaml.cs
using MyUserApp.ViewModels;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyUserApp.Views
{
    /// <summary>
    /// Interaction logic for the ImageEditorView.
    /// This view uses a SkiaSharp SKElement for high-performance drawing.
    /// The code-behind forwards user input to the ViewModel and handles
    /// redraw requests from the ViewModel.
    /// </summary>
    public partial class ImageEditorView : UserControl
    {
        public ImageEditorView()
        {
            InitializeComponent();
            this.DataContextChanged += ImageEditorView_DataContextChanged;
        }

        private void ImageEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ImageEditorViewModel oldVm)
            {
                oldVm.RequestCanvasInvalidation -= OnRequestCanvasInvalidation;
            }
            if (e.NewValue is ImageEditorViewModel newVm)
            {
                newVm.RequestCanvasInvalidation += OnRequestCanvasInvalidation;
            }
        }

        private void OnRequestCanvasInvalidation()
        {
            this.SkiaElement.InvalidateVisual();
        }

        private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;

        private void CanvasView_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            ViewModel?.Draw(e.Surface, e.Info);
        }

        private void CanvasView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;
            float zoomFactor = e.Delta > 0 ? 1.2f : 1 / 1.2f;
            Point mousePos = e.GetPosition(SkiaElement);
            ViewModel.Zoom(zoomFactor, (float)mousePos.X, (float)mousePos.Y);
        }

        private void CanvasView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            // Set focus so the element can receive key presses.
            SkiaElement.Focus();
            SkiaElement.CaptureMouse();
            Point pos = e.GetPosition(SkiaElement);
            if (Keyboard.IsKeyDown(Key.Space))
            {
                ViewModel.StartPan((float)pos.X, (float)pos.Y);
            }
            else
            {
                ViewModel.StartDrawing((float)pos.X, (float)pos.Y);
            }
        }

        private void CanvasView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && ViewModel != null)
            {
                Point pos = e.GetPosition(SkiaElement);
                ViewModel.UpdateInteraction((float)pos.X, (float)pos.Y);
            }
        }

        private void CanvasView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            SkiaElement.ReleaseMouseCapture();
            Point pos = e.GetPosition(SkiaElement);
            ViewModel.EndInteraction((float)pos.X, (float)pos.Y);
        }

        /// <summary>
        /// Handles key presses when the canvas is focused.
        /// This provides keyboard shortcuts for common actions.
        /// </summary>
        private void SkiaElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null) return;

            // ===================================================================
            // ==      NEW: Added a check for the Ctrl+S save shortcut        ==
            // ===================================================================
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                // Execute the public SaveCommand on the ViewModel.
                ViewModel.SaveCommand.Execute(null);

                // Mark the event as handled to prevent any further processing.
                e.Handled = true;
            }
            // ===================================================================
            else if (e.Key == Key.Delete)
            {
                // The existing delete logic.
                ViewModel.DeleteSelectedAnnotation();
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}