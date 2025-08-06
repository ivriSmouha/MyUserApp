using System;
using System.Globalization;
using System.Windows.Data;

namespace MyUserApp.Converters
{
    public class SelectedToStrokeThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return 4.0; // Thicker border when selected
            }
            return 2.0; // Default thickness
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}