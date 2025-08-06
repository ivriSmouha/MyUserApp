using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A MultiValueConverter that calculates the absolute pixel diameter (Width and Height) for an annotation ellipse.
    /// This converter ensures that annotations remain perfect circles regardless of window resizing by always
    /// using the container width as the reference dimension for both width and height calculations.
    /// </summary>
    public class RelativeSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a relative radius to an absolute pixel diameter for ellipse sizing.
        /// </summary>
        /// <param name="values">Array containing: [0] relative radius (0.0-1.0), [1] container width in pixels</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Optional converter parameter</param>
        /// <param name="culture">The culture information</param>
        /// <returns>The absolute pixel diameter (width or height) for the ellipse</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate that we have exactly 2 valid double values
            if (values == null || values.Length < 2 || values.Any(v => v == DependencyProperty.UnsetValue || !(v is double)))
            {
                // Return UnsetValue to prevent drawing a zero-size ellipse in the corner
                return DependencyProperty.UnsetValue;
            }

            var relativeRadius = (double)values[0];     // Radius as fraction of container width (0.0 to 1.0)
            var containerWidth = (double)values[1];     // Container width in pixels

            // Ensure the container has valid dimensions
            if (containerWidth <= 0)
            {
                return DependencyProperty.UnsetValue;
            }

            // Calculate the diameter in pixels
            // The diameter is twice the radius, scaled by the container's width
            // We always use containerWidth (not height) to ensure circles remain circular
            // even when the image aspect ratio differs from the container aspect ratio
            double absoluteDiameter = relativeRadius * containerWidth * 2;

            return absoluteDiameter;
        }

        /// <summary>
        /// ConvertBack is not implemented as this converter only works one-way (from relative to absolute size).
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("RelativeSizeConverter only supports one-way conversion.");
        }
    }
}