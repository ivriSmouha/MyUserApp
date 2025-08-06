// File: MyUserApp/Converters/SelectedToBrushConverter.cs
using MyUserApp.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyUserApp.Converters
{
    // This converter highlights a selected annotation
    public class SelectedToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is AuthorType author) || !(values[1] is bool isSelected))
                return Brushes.Transparent;

            if (isSelected) return Brushes.LimeGreen;

            switch (author)
            {
                case AuthorType.Inspector: return Brushes.DodgerBlue;
                case AuthorType.Verifier: return Brushes.Yellow;
                case AuthorType.AI: return Brushes.Red;
                default: return Brushes.White;
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}