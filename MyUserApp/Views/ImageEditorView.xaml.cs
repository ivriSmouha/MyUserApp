// File: MyUserApp/Views/ImageEditorView.xaml.cs
using MyUserApp.ViewModels;
using SkiaSharp.Views.WPF;

// ===================================================================
// ==   CORRECTION: Added the namespace for SKPaintSurfaceEventArgs   ==
// ===================================================================
// This class lives in a different namespace than the SKElement control itself.
// Adding this using directive resolves the compiler error.
using SkiaSharp.Views.Desktop;
// ===================================================================

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
            // We hook into the DataContextChanged event to know when the ViewModel is ready.
            this.DataContextChanged += ImageEditorView_DataContextChanged;
        }

        /// <summary>
        /// This event fires when the ViewModel is assigned to the DataContext.
        /// We use it to subscribe to the ViewModel's redraw requests.
        /// </summary>
        private void ImageEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ImageEditorViewModel oldVm)
            {
                // Unsubscribe from the old ViewModel to prevent memory leaks.
                oldVm.RequestCanvasInvalidation -= OnRequestCanvasInvalidation;
            }
            if (e.NewValue is ImageEditorViewModel newVm)
            {
                // Subscribe to the new ViewModel's event.
                newVm.RequestCanvasInvalidation += OnRequestCanvasInvalidation;
            }
        }

        /// <summary>
        /// This method is called when the ViewModel signals that the canvas needs to be redrawn.
        /// </summary>
        private void OnRequestCanvasInvalidation()
        {
            // InvalidateVisual is the method that tells the SKElement to fire its PaintSurface event.
            this.SkiaElement.InvalidateVisual();
        }

        /// <summary>
        /// Gets the ViewModel associated with this View.
        /// </summary>
        private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;

        /// <summary>
        /// This is the core rendering event for the SkiaSharp canvas.
        /// It fires whenever the canvas needs to be redrawn.
        /// </summary>
        private void CanvasView_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            ViewModel?.Draw(e.Surface, e.Info);
        }

        /// <summary>
        /// Handles the mouse wheel event for zooming.
        /// </summary>
        private void CanvasView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;
            float zoomFactor = e.Delta > 0 ? 1.2f : 1 / 1.2f;
            Point mousePos = e.GetPosition(SkiaElement);
            ViewModel.Zoom(zoomFactor, (float)mousePos.X, (float)mousePos.Y);
        }

        /// <summary>
        /// Handles the start of a mouse drag operation.
        /// </summary>
        private void CanvasView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            // Capture the mouse to ensure we get MouseUp events even if the cursor leaves the control.
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

        /// <summary>
        /// Handles mouse movement during a drag operation.
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
        /// Handles the end of a mouse drag operation.
        /// </summary>
        private void CanvasView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            // Release the mouse capture.
            SkiaElement.ReleaseMouseCapture();
            Point pos = e.GetPosition(SkiaElement);
            ViewModel.EndInteraction((float)pos.X, (float)pos.Y);
        }
    }
}