using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A MultiValueConverter that calculates the absolute pixel position for annotations,
    /// taking into account the actual image content area within the Image element.
    /// This handles cases where Stretch="Uniform" creates letterboxing around the image.
    /// </summary>
    public class ImageAwarePositionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts relative coordinates to absolute pixel positions within the actual image content area.
        /// </summary>
        /// <param name="values">Array containing: [0] relative center (0.0-1.0), [1] relative radius (0.0-1.0), [2] Image element, [3] coordinate type ("X" or "Y")</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Coordinate type parameter ("X" or "Y")</param>
        /// <param name="culture">The culture information</param>
        /// <returns>The absolute pixel position for the annotation</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate input parameters
            if (values == null || values.Length < 3)
                return DependencyProperty.UnsetValue;

            if (!(values[0] is double relativeCenter) || !(values[1] is double relativeRadius) || !(values[2] is Image imageElement))
                return DependencyProperty.UnsetValue;

            // Get the coordinate type from parameter
            string coordType = parameter as string ?? "X";

            // Calculate the actual image content bounds within the Image element
            var contentBounds = GetImageContentBounds(imageElement);
            if (contentBounds == Rect.Empty)
                return DependencyProperty.UnsetValue;

            double absoluteCenter, absoluteRadius;

            if (coordType == "Y")
            {
                // Calculate Y coordinate (Top position)
                absoluteCenter = contentBounds.Top + (relativeCenter * contentBounds.Height);
                absoluteRadius = relativeRadius * contentBounds.Width; // Use width for consistent scaling
            }
            else
            {
                // Calculate X coordinate (Left position)
                absoluteCenter = contentBounds.Left + (relativeCenter * contentBounds.Width);
                absoluteRadius = relativeRadius * contentBounds.Width;
            }

            // Return the position for the ellipse edge (subtract radius from center)
            return absoluteCenter - absoluteRadius;
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
            throw new NotImplementedException("ImageAwarePositionConverter only supports one-way conversion.");
        }
    }
}