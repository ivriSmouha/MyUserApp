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
    /// A service responsible for exporting the edited images with their annotations.
    /// </summary>
    public class ImageExportService
    {
        /// <summary>
        /// Asynchronously renders and saves all images with their corresponding annotations.
        /// </summary>
        /// <param name="report">The original inspection report.</param>
        /// <param name="allAnnotations">A dictionary mapping image paths to their annotations.</param>
        /// <returns>The path to the output directory.</returns>
        public async Task<string> ExportImagesAsync(InspectionReportModel report, Dictionary<string, ObservableCollection<AnnotationModel>> allAnnotations)
        {
            // Create a unique, timestamped output directory for the edited report.
            string sanitizedProjectName = string.Join("_", report.ProjectName.Split(Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Edited Reports", $"{sanitizedProjectName}_{timestamp}");
            Directory.CreateDirectory(outputDirectory);

            // Process each image that has annotations.
            foreach (var entry in allAnnotations)
            {
                string originalImagePath = entry.Key;
                var annotations = entry.Value;

                // Skip images that have no annotations.
                if (annotations == null || annotations.Count == 0) continue;

                // This will run the CPU and GPU-intensive rendering on a background thread
                // to keep the UI responsive.
                await Task.Run(() =>
                {
                    // Load the original bitmap image.
                    BitmapImage originalBitmap = new BitmapImage(new Uri(originalImagePath));

                    // Create a DrawingVisual to compose our final image.
                    var drawingVisual = new DrawingVisual();
                    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                    {
                        // Draw the original image first.
                        drawingContext.DrawImage(originalBitmap, new Rect(0, 0, originalBitmap.PixelWidth, originalBitmap.PixelHeight));

                        // Draw each annotation on top of the image.
                        foreach (var annotation in annotations)
                        {
                            var brush = GetBrushForAuthor(annotation.Author);
                            double absoluteRadius = annotation.Radius * originalBitmap.PixelWidth;
                            Point center = new Point(annotation.CenterX * originalBitmap.PixelWidth, annotation.CenterY * originalBitmap.PixelHeight);

                            // Draw the ellipse with a semi-transparent fill and a solid border.
                            drawingContext.DrawEllipse(new SolidColorBrush(brush.Color) { Opacity = 0.2 },
                                                       new Pen(brush, 4),
                                                       center,
                                                       absoluteRadius,
                                                       absoluteRadius);
                        }
                    }

                    // Render the DrawingVisual to a bitmap.
                    var renderTargetBitmap = new RenderTargetBitmap(originalBitmap.PixelWidth, originalBitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(drawingVisual);

                    // Encode the final bitmap as a JPEG file.
                    var jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                    // Save the file to the output directory.
                    string outputFileName = Path.GetFileName(originalImagePath);
                    string outputFilePath = Path.Combine(outputDirectory, outputFileName);
                    using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
                    {
                        jpegEncoder.Save(fileStream);
                    }
                });
            }

            return outputDirectory;
        }

        private SolidColorBrush GetBrushForAuthor(AuthorType author)
        {
            switch (author)
            {
                case AuthorType.Inspector: return Brushes.DodgerBlue;
                case AuthorType.Verifier: return Brushes.Yellow;
                case AuthorType.AI: return Brushes.Red;
                default: return Brushes.White;
            }
        }
    }
}