using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A MultiValueConverter that calculates the absolute pixel diameter for annotations,
    /// taking into account the actual image content area within the Image element.
    /// This ensures annotations remain circular even with letterboxing from Stretch="Uniform".
    /// </summary>
    public class ImageAwareSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a relative radius to an absolute pixel diameter based on the actual image content area.
        /// </summary>
        /// <param name="values">Array containing: [0] relative radius (0.0-1.0), [1] Image element</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Optional converter parameter</param>
        /// <param name="culture">The culture information</param>
        /// <returns>The absolute pixel diameter for the annotation ellipse</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate input parameters
            if (values == null || values.Length < 2)
                return DependencyProperty.UnsetValue;

            if (!(values[0] is double relativeRadius) || !(values[1] is Image imageElement))
                return DependencyProperty.UnsetValue;

            // Calculate the actual image content bounds within the Image element
            var contentBounds = GetImageContentBounds(imageElement);
            if (contentBounds == Rect.Empty)
                return DependencyProperty.UnsetValue;

            // Calculate the diameter based on the content width for consistent circular shape
            double absoluteDiameter = relativeRadius * contentBounds.Width * 2;

            return absoluteDiameter;
        }

        /// <summary>
        /// Calculates the actual bounds of the image content within the Image element,
        /// accounting for Stretch="Uniform" behavior which may add letterboxing.
        /// </summary>
        /// <param name="imageElement">The Image element containing the displayed image</param>
        /// <returns>Rectangle representing the actual image content bounds</returns>
        private Rect GetImageContentBounds(Image imageElement)
        {
            if (imageElement?.Source is not BitmapSource bitmapSource)
                return Rect.Empty;

            // Get the dimensions of the Image element and the actual bitmap
            double elementWidth = imageElement.ActualWidth;
            double elementHeight = imageElement.ActualHeight;
            double imageWidth = bitmapSource.PixelWidth;
            double imageHeight = bitmapSource.PixelHeight;

            // Check for valid dimensions
            if (elementWidth <= 0 || elementHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
                return Rect.Empty;

            // Calculate the aspect ratios
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
        /// ConvertBack is not implemented as this converter only works one-way.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ImageAwareSizeConverter only supports one-way conversion.");
        }
    }
}