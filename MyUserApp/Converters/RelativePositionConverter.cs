using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A MultiValueConverter that calculates an absolute pixel position (Left or Top) for an annotation.
    /// It now only requires three values, as the aspect ratio is handled by the Viewbox.
    /// 1. The relative center coordinate (CenterX or CenterY from 0.0 to 1.0).
    /// 2. The relative radius (from 0.0 to 1.0).
    /// 3. The actual pixel size of the container (ActualWidth or ActualHeight).
    /// </summary>
    public class RelativePositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 || values.Any(v => v == DependencyProperty.UnsetValue || !(v is double)))
            {
                return DependencyProperty.UnsetValue;
            }

            var relativeCenter = (double)values[0];
            var relativeRadius = (double)values[1];
            var containerSize = (double)values[2];

            if (containerSize <= 0)
            {
                return DependencyProperty.UnsetValue;
            }

            // The container for the radius is now the same as the container for the center.
            double absoluteCenter = relativeCenter * containerSize;
            double absoluteRadius = relativeRadius * containerSize;

            return absoluteCenter - absoluteRadius;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}