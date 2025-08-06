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
    /// Debug version of export service that adds visual debugging markers to help identify coordinate issues.
    /// </summary>
    public class ImageExportService
    {
        /// <summary>
        /// Export with visual debug markers to help identify coordinate differences.
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
        /// Export with visual debug markers to help identify exactly where coordinates are being placed.
        /// </summary>
        private void ExportWithVisualDebugMarkers(string originalImagePath, ObservableCollection<AnnotationModel> annotations, string outputDirectory)
        {
            try
            {
                BitmapImage originalBitmap = new BitmapImage(new Uri(originalImagePath));

                double originalWidth = originalBitmap.PixelWidth;
                double originalHeight = originalBitmap.PixelHeight;

                // Export at the same scale as the previous version that had close coordinates
                double scale = 4.87; // From your debug output
                int exportWidth = (int)(originalWidth * scale);
                int exportHeight = (int)(originalHeight * scale);

                System.Diagnostics.Debug.WriteLine($"\n=== VISUAL DEBUG EXPORT ===");
                System.Diagnostics.Debug.WriteLine($"Original: {originalWidth} x {originalHeight}");
                System.Diagnostics.Debug.WriteLine($"Export: {exportWidth} x {exportHeight}");

                var drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    // Draw the image
                    drawingContext.DrawImage(originalBitmap, new Rect(0, 0, exportWidth, exportHeight));

                    // Add coordinate grid for debugging
                    DrawDebugGrid(drawingContext, exportWidth, exportHeight);

                    // Draw each annotation with debug markers
                    int annotationIndex = 0;
                    foreach (var annotation in annotations)
                    {
                        double absoluteCenterX = annotation.CenterX * exportWidth;
                        double absoluteCenterY = annotation.CenterY * exportHeight;
                        double absoluteRadius = annotation.Radius * exportWidth;

                        System.Diagnostics.Debug.WriteLine($"Export Debug Annotation #{annotationIndex}:");
                        System.Diagnostics.Debug.WriteLine($"  Center: ({absoluteCenterX:F2}, {absoluteCenterY:F2}), Radius: {absoluteRadius:F2}");

                        // Draw the main annotation
                        var brush = GetBrushForAuthor(annotation.Author);
                        var fillBrush = new SolidColorBrush(brush.Color) { Opacity = 0.2 };
                        var borderPen = new Pen(brush, 4);

                        Point center = new Point(absoluteCenterX, absoluteCenterY);
                        drawingContext.DrawEllipse(fillBrush, borderPen, center, absoluteRadius, absoluteRadius);

                        // Add debug markers
                        DrawDebugMarkers(drawingContext, absoluteCenterX, absoluteCenterY, absoluteRadius, annotationIndex);

                        annotationIndex++;
                    }
                }

                var renderTargetBitmap = new RenderTargetBitmap(exportWidth, exportHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);

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

            // Draw vertical lines every 10% of width
            for (int i = 1; i < 10; i++)
            {
                double x = width * (i / 10.0);
                drawingContext.DrawLine(gridPen, new Point(x, 0), new Point(x, height));
            }

            // Draw horizontal lines every 10% of height
            for (int i = 1; i < 10; i++)
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
            // Draw a small crosshair at the exact center
            var crossPen = new Pen(Brushes.Lime, 2);
            double crossSize = 10;
            drawingContext.DrawLine(crossPen,
                new Point(centerX - crossSize, centerY),
                new Point(centerX + crossSize, centerY));
            drawingContext.DrawLine(crossPen,
                new Point(centerX, centerY - crossSize),
                new Point(centerX, centerY + crossSize));

            // Add text label with coordinates
            var text = new FormattedText(
                $"#{index}: ({centerX:F0}, {centerY:F0})",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12,
                Brushes.White,
                96);

            // Position text above the annotation
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