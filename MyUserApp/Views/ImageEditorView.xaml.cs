using MyUserApp.ViewModels;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyUserApp.Views
{
    /// <summary>
    /// The code-behind for the ImageEditorView.xaml.
    /// This file acts as a bridge between the high-performance SkiaSharp canvas (SKElement) and the ViewModel.
    /// Its primary responsibility is to forward user input events (mouse, keyboard) to the ViewModel
    /// and to trigger a canvas redraw when the ViewModel requests it.
    /// </summary>
    public partial class ImageEditorView : UserControl
    {
        public ImageEditorView()
        {
            InitializeComponent();
            // Subscribe to the DataContextChanged event to manage event subscriptions.
            this.DataContextChanged += ImageEditorView_DataContextChanged;
        }

        /// <summary>
        /// Manages the subscription to the ViewModel's redraw request event.
        /// This is crucial to prevent memory leaks by unsubscribing from the old ViewModel when the DataContext changes.
        /// </summary>
        private void ImageEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from the old ViewModel, if it exists.
            if (e.OldValue is ImageEditorViewModel oldVm)
            {
                oldVm.RequestCanvasInvalidation -= OnRequestCanvasInvalidation;
            }
            // Subscribe to the new ViewModel.
            if (e.NewValue is ImageEditorViewModel newVm)
            {
                newVm.RequestCanvasInvalidation += OnRequestCanvasInvalidation;
            }
        }

        /// <summary>
        /// This method is called when the ViewModel requests a redraw of the canvas.
        /// </summary>
        private void OnRequestCanvasInvalidation()
        {
            // InvalidateVisual tells the SKElement that its content is outdated and needs to be repainted.
            this.SkiaElement.InvalidateVisual();
        }

        // A private helper property to get the DataContext as an ImageEditorViewModel.
        private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;

        /// <summary>
        /// This event is the main entry point for all drawing on the canvas.
        /// It calls the ViewModel's Draw method, passing the surface to draw on.
        /// </summary>
        private void CanvasView_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            ViewModel?.Draw(e.Surface, e.Info);
        }

        /// <summary>
        /// Forwards mouse wheel events to the ViewModel to handle zooming.
        /// </summary>
        private void CanvasView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;
            // Calculate the zoom factor and get the mouse position as the zoom anchor point.
            float zoomFactor = e.Delta > 0 ? 1.2f : 1 / 1.2f;
            Point mousePos = e.GetPosition(SkiaElement);
            ViewModel.Zoom(zoomFactor, (float)mousePos.X, (float)mousePos.Y);
        }

        /// <summary>
        /// Handles the start of a mouse interaction, determining whether to pan or draw.
        /// </summary>
        private void CanvasView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            // Set focus to the canvas to enable keyboard shortcuts.
            SkiaElement.Focus();
            SkiaElement.CaptureMouse();
            Point pos = e.GetPosition(SkiaElement);

            // If the Spacebar is held down, initiate a pan operation.
            if (Keyboard.IsKeyDown(Key.Space))
            {
                ViewModel.StartPan((float)pos.X, (float)pos.Y);
            }
            // Otherwise, start drawing an annotation.
            else
            {
                ViewModel.StartDrawing((float)pos.X, (float)pos.Y);
            }
        }

        /// <summary>
        /// Forwards mouse movement to the ViewModel while the left button is held down.
        /// </summary>
        private void CanvasView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && ViewModel != null)
            {
                Point pos = e.GetPosition(SkiaElement);
                ViewModel.UpdateInteraction((float)pos.X, (float)pos.Y);
            }
        }

        /// <summary>
        /// Forwards the mouse up event to the ViewModel to finalize the interaction (e.g., finish drawing).
        /// </summary>
        private void CanvasView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            SkiaElement.ReleaseMouseCapture();
            Point pos = e.GetPosition(SkiaElement);
            ViewModel.EndInteraction((float)pos.X, (float)pos.Y);
        }

        /// <summary>
        /// Handles key presses when the canvas has focus to provide keyboard shortcuts.
        /// </summary>
        private void SkiaElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null) return;

            // Provides the Ctrl+S shortcut to save the report.
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                ViewModel.SaveCommand.Execute(null);
                e.Handled = true; // Mark the event as handled to prevent further processing.
            }
            // Provides the Delete key shortcut to remove the selected annotation.
            else if (e.Key == Key.Delete)
            {
                ViewModel.DeleteSelectedAnnotation();
                e.Handled = true;
            }
        }
    }
}
