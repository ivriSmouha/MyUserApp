// File: MyUserApp/Converters/BrightnessToOverlayConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyUserApp.Converters
{
    public class BrightnessToOverlayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double brightness)) return null;
            string requestedValue = parameter as string;

            if (requestedValue == "Brush")
            {
                return brightness > 1.0 ? Brushes.White : Brushes.Black;
            }

            if (requestedValue == "Opacity")
            {
                if (brightness > 1.0) return Math.Min((brightness - 1.0) * 0.7, 1.0);
                else return 1.0 - brightness;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}