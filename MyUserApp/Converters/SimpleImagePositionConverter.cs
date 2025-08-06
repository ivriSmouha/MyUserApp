using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MyUserApp.Converters
{
    /// <summary>
    /// Simplified converter that calculates annotation positions with perfect consistency.
    /// Works reliably with any image aspect ratio and window size.
    /// </summary>
    public class SimpleImagePositionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts relative annotation coordinates to absolute pixel positions.
        /// </summary>
        /// <param name="values">Array containing: [0] relative center coordinate, [1] relative radius, [2] Image element, [3] coordinate type</param>
        /// <param name="targetType">Target property type</param>
        /// <param name="parameter">Parameter indicating "X" or "Y" coordinate</param>
        /// <param name="culture">Culture information</param>
        /// <returns>Absolute pixel position for the annotation</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate input parameters
            if (values == null || values.Length < 3)
                return DependencyProperty.UnsetValue;

            if (!(values[0] is double relativeCenter) ||
                !(values[1] is double relativeRadius) ||
                !(values[2] is Image imageElement))
                return DependencyProperty.UnsetValue;

            // Get coordinate type (X or Y)
            string coordType = parameter?.ToString() ?? "X";

            // Calculate image content bounds
            var contentBounds = CalculateImageContentBounds(imageElement);
            if (contentBounds == Rect.Empty)
                return DependencyProperty.UnsetValue;

            double absoluteCenter, absoluteRadius;

            if (coordType == "Y")
            {
                // Calculate Y coordinate (Top position)
                absoluteCenter = contentBounds.Top + (relativeCenter * contentBounds.Height);
                absoluteRadius = relativeRadius * contentBounds.Width; // Always use width for consistent scaling
            }
            else
            {
                // Calculate X coordinate (Left position)
                absoluteCenter = contentBounds.Left + (relativeCenter * contentBounds.Width);
                absoluteRadius = relativeRadius * contentBounds.Width;
            }

            // Return the edge position (center minus radius)
            return absoluteCenter - absoluteRadius;
        }

        /// <summary>
        /// Calculates the actual bounds of image content within the Image element.
        /// Handles letterboxing from Stretch="Uniform" correctly for any aspect ratio.
        /// </summary>
        /// <param name="imageElement">The Image element displaying the image</param>
        /// <returns>Rectangle representing the actual image content area</returns>
        private static Rect CalculateImageContentBounds(Image imageElement)
        {
            // Validate image element and source
            if (imageElement?.Source is not BitmapSource bitmapSource)
                return Rect.Empty;

            double elementWidth = imageElement.ActualWidth;
            double elementHeight = imageElement.ActualHeight;
            double imageWidth = bitmapSource.PixelWidth;
            double imageHeight = bitmapSource.PixelHeight;

            // Ensure all dimensions are valid
            if (elementWidth <= 0 || elementHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
                return Rect.Empty;

            // Calculate aspect ratios
            double elementAspect = elementWidth / elementHeight;
            double imageAspect = imageWidth / imageHeight;

            double contentWidth, contentHeight, offsetX, offsetY;

            if (imageAspect > elementAspect)
            {
                // Image is wider than container - fit to width, letterbox top/bottom
                contentWidth = elementWidth;
                contentHeight = elementWidth / imageAspect;
                offsetX = 0;
                offsetY = (elementHeight - contentHeight) / 2;
            }
            else
            {
                // Image is taller than container - fit to height, letterbox left/right  
                contentHeight = elementHeight;
                contentWidth = elementHeight * imageAspect;
                offsetX = (elementWidth - contentWidth) / 2;
                offsetY = 0;
            }

            return new Rect(offsetX, offsetY, contentWidth, contentHeight);
        }

        /// <summary>
        /// ConvertBack is not implemented as this is a one-way converter.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SimpleImagePositionConverter only supports one-way conversion.");
        }
    }
}