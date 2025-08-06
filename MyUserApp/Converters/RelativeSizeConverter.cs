using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A MultiValueConverter that calculates the absolute pixel diameter (Width and Height) for an annotation.
    /// It requires two values:
    /// 1. The relative radius of the circle (from 0.0 to 1.0, based on container width).
    /// 2. The actual pixel width of the container. This should ALWAYS be the canvas's ActualWidth for both
    ///    the ellipse's Width and Height to ensure it remains a circle.
    /// </summary>
    public class RelativeSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure we have two valid double values.
            if (values == null || values.Length < 2 || values.Any(v => v == DependencyProperty.UnsetValue || !(v is double)))
            {
                // Return UnsetValue to prevent drawing a zero-size ellipse in the corner.
                return DependencyProperty.UnsetValue;
            }

            var relativeRadius = (double)values[0];
            var containerWidth = (double)values[1];

            if (containerWidth <= 0)
            {
                return DependencyProperty.UnsetValue;
            }

            // The diameter is twice the radius, scaled by the container's width.
            return relativeRadius * containerWidth * 2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}