using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MyUserApp.Converters
{
    /// <summary>
    /// Simplified converter that calculates annotation diameters with perfect consistency.
    /// Ensures annotations remain perfectly circular regardless of image aspect ratio or window size.
    /// </summary>
    public class SimpleImageSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a relative radius to an absolute pixel diameter.
        /// </summary>
        /// <param name="values">Array containing: [0] relative radius, [1] Image element</param>
        /// <param name="targetType">Target property type</param>
        /// <param name="parameter">Optional converter parameter</param>
        /// <param name="culture">Culture information</param>
        /// <returns>Absolute pixel diameter for the annotation circle</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate input parameters
            if (values == null || values.Length < 2)
                return DependencyProperty.UnsetValue;

            if (!(values[0] is double relativeRadius) || !(values[1] is Image imageElement))
                return DependencyProperty.UnsetValue;

            // Calculate image content bounds
            var contentBounds = CalculateImageContentBounds(imageElement);
            if (contentBounds == Rect.Empty)
                return DependencyProperty.UnsetValue;

            // Calculate diameter using content width for consistent circular shape
            // Always use width to ensure circles don't become ellipses
            double absoluteDiameter = relativeRadius * contentBounds.Width * 2;

            return absoluteDiameter;
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
            throw new NotImplementedException("SimpleImageSizeConverter only supports one-way conversion.");
        }
    }
}