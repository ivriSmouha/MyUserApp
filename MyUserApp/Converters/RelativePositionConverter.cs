using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A MultiValueConverter that calculates the absolute pixel position (Left or Top) for an annotation ellipse.
    /// This converter takes relative coordinates (0.0 to 1.0) and converts them to absolute pixel positions
    /// on the canvas, accounting for the circle's radius to position the ellipse correctly.
    /// </summary>
    public class RelativePositionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts relative center coordinates and radius to absolute pixel position for ellipse placement.
        /// </summary>
        /// <param name="values">Array containing: [0] relative center (0.0-1.0), [1] relative radius (0.0-1.0), [2] container dimension in pixels</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Optional converter parameter</param>
        /// <param name="culture">The culture information</param>
        /// <returns>The absolute pixel position for the ellipse's left or top edge</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate that we have exactly 3 valid double values
            if (values == null || values.Length < 3 || values.Any(v => v == DependencyProperty.UnsetValue || !(v is double)))
            {
                return DependencyProperty.UnsetValue;
            }

            var relativeCenter = (double)values[0];     // Center position as fraction of container (0.0 to 1.0)
            var relativeRadius = (double)values[1];     // Radius as fraction of container width (0.0 to 1.0)
            var containerSize = (double)values[2];      // Actual pixel dimension of the container

            // Ensure the container has valid dimensions
            if (containerSize <= 0)
            {
                return DependencyProperty.UnsetValue;
            }

            // Convert relative coordinates to absolute pixel coordinates
            double absoluteCenter = relativeCenter * containerSize;     // Center position in pixels
            double absoluteRadius = relativeRadius * containerSize;     // Radius in pixels

            // Calculate the position for the ellipse's edge (left or top)
            // Since Canvas.Left and Canvas.Top specify the top-left corner of the ellipse,
            // we subtract the radius from the center to get the correct positioning
            return absoluteCenter - absoluteRadius;
        }

        /// <summary>
        /// ConvertBack is not implemented as this converter only works one-way (from relative to absolute coordinates).
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("RelativePositionConverter only supports one-way conversion.");
        }
    }
}