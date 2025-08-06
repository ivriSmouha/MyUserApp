// File: MyUserApp/Converters/SelectedToBorderBrushConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyUserApp.Converters
{
    // This converter is for the image thumbnails on the left
    public class SelectedToBorderBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return Brushes.Transparent;

            string item = values[0].ToString();
            string selectedItem = values[1].ToString();

            return item == selectedItem ? Brushes.DodgerBlue : Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}