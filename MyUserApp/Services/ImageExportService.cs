// File: MyUserApp/Services/ImageExportService.cs
using MyUserApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyUserApp.Services
{
    /// <summary>
    /// Service to export images with annotations. The debug markers help verify coordinate accuracy.
    /// </summary>
    public class ImageExportService
    {
        /// <summary>
        /// Exports images with annotations and visual debug markers.
        /// </summary>
        public async Task<string> ExportImagesAsync(InspectionReportModel report, Dictionary<string, ObservableCollection<AnnotationModel>> allAnnotations)
        {
            string sanitizedProjectName = string.Join("_", report.ProjectName.Split(Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exported Reports", $"{sanitizedProjectName}_{timestamp}");
            Directory.CreateDirectory(outputDirectory);

            foreach (var entry in allAnnotations)
            {
                string originalImagePath = entry.Key;
                var annotations = entry.Value;

                if (annotations == null || annotations.Count == 0) continue;

                await Task.Run(() =>
                {
                    ExportWithVisualDebugMarkers(originalImagePath, annotations, outputDirectory);
                });
            }

            return outputDirectory;
        }

        /// <summary>
        /// Renders and saves a single image with its annotations and debug information.
        /// </summary>
        private void ExportWithVisualDebugMarkers(string originalImagePath, ObservableCollection<AnnotationModel> annotations, string outputDirectory)
        {
            try
            {
                BitmapImage originalBitmap = new BitmapImage(new Uri(originalImagePath));

                // ===================================================================
                // ==          FIX FOR EXPORT ACCURACY (Bug #2)                     ==
                // ===================================================================
                // The root cause of the alignment issue was using an arbitrary scale.
                // The correct approach is to render the export at the image's
                // NATIVE resolution. We get this directly from the bitmap's pixel dimensions.

                // REMOVED: The incorrect hard-coded scale factor.
                // double scale = 4.87; 

                // CHANGED: The export dimensions now match the original image EXACTLY.
                int exportWidth = originalBitmap.PixelWidth;
                int exportHeight = originalBitmap.PixelHeight;
                // ===================================================================

                System.Diagnostics.Debug.WriteLine($"\n=== VISUAL DEBUG EXPORT ===");
                System.Diagnostics.Debug.WriteLine($"Original: {originalBitmap.PixelWidth} x {originalBitmap.PixelHeight}");
                System.Diagnostics.Debug.WriteLine($"Export Target: {exportWidth} x {exportHeight}");

                var drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    // Draw the base image to fill the entire render target.
                    drawingContext.DrawImage(originalBitmap, new Rect(0, 0, exportWidth, exportHeight));

                    // Add a coordinate grid for debugging.
                    DrawDebugGrid(drawingContext, exportWidth, exportHeight);

                    // Draw each annotation with debug markers.
                    int annotationIndex = 0;
                    foreach (var annotation in annotations)
                    {
                        // Now that exportWidth/Height match the original image, these
                        // calculations will produce the correct pixel coordinates.
                        double absoluteCenterX = annotation.CenterX * exportWidth;
                        double absoluteCenterY = annotation.CenterY * exportHeight;
                        double absoluteRadius = annotation.Radius * exportWidth; // Use width for consistent radius scaling

                        System.Diagnostics.Debug.WriteLine($"Export Debug Annotation #{annotationIndex}:");
                        System.Diagnostics.Debug.WriteLine($"  Center: ({absoluteCenterX:F2}, {absoluteCenterY:F2}), Radius: {absoluteRadius:F2}");

                        // Draw the main annotation.
                        var brush = GetBrushForAuthor(annotation.Author);
                        var fillBrush = new SolidColorBrush(brush.Color) { Opacity = 0.2 };
                        var borderPen = new Pen(brush, 4); // Stroke thickness can be adjusted here

                        Point center = new Point(absoluteCenterX, absoluteCenterY);
                        drawingContext.DrawEllipse(fillBrush, borderPen, center, absoluteRadius, absoluteRadius);

                        // Add debug markers for verification.
                        DrawDebugMarkers(drawingContext, absoluteCenterX, absoluteCenterY, absoluteRadius, annotationIndex);

                        annotationIndex++;
                    }
                }

                // Render the visual content to a bitmap.
                var renderTargetBitmap = new RenderTargetBitmap(exportWidth, exportHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);

                // Encode the bitmap as a JPEG.
                var jpegEncoder = new JpegBitmapEncoder { QualityLevel = 95 };
                jpegEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                string outputFileName = Path.GetFileNameWithoutExtension(originalImagePath) + "_debug.jpg";
                string outputFilePath = Path.Combine(outputDirectory, outputFileName);
                using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    jpegEncoder.Save(fileStream);
                }

                System.Diagnostics.Debug.WriteLine($"Exported debug version: {outputFileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export error: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw a coordinate grid to help visualize the coordinate system.
        /// </summary>
        private void DrawDebugGrid(DrawingContext drawingContext, double width, double height)
        {
            var gridPen = new Pen(Brushes.Gray, 1) { DashStyle = DashStyles.Dot };
            for (int i = 1; i < 10; i++) // Vertical lines
            {
                double x = width * (i / 10.0);
                drawingContext.DrawLine(gridPen, new Point(x, 0), new Point(x, height));
            }
            for (int i = 1; i < 10; i++) // Horizontal lines
            {
                double y = height * (i / 10.0);
                drawingContext.DrawLine(gridPen, new Point(0, y), new Point(width, y));
            }
        }

        /// <summary>
        /// Draw debug markers showing exact coordinate positions.
        /// </summary>
        private void DrawDebugMarkers(DrawingContext drawingContext, double centerX, double centerY, double radius, int index)
        {
            var crossPen = new Pen(Brushes.Lime, 2);
            double crossSize = 10;
            drawingContext.DrawLine(crossPen, new Point(centerX - crossSize, centerY), new Point(centerX + crossSize, centerY));
            drawingContext.DrawLine(crossPen, new Point(centerX, centerY - crossSize), new Point(centerX, centerY + crossSize));

            var text = new FormattedText(
                $"#{index}: ({centerX:F0}, {centerY:F0})",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16, // Slightly larger text for better readability on high-res images
                Brushes.White,
                96);

            drawingContext.DrawText(text, new Point(centerX - text.Width / 2, centerY - radius - 20));
        }

        private SolidColorBrush GetBrushForAuthor(AuthorType author)
        {
            return author switch
            {
                AuthorType.Inspector => Brushes.DodgerBlue,
                AuthorType.Verifier => Brushes.Yellow,
                AuthorType.AI => Brushes.Red,
                _ => Brushes.White
            };
        }
    }
}