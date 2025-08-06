using MyUserApp.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyUserApp.Converters
{
    /// <summary>
    /// Converts an AuthorType enum value into a colored Brush for the UI.
    /// This allows the View to display different colors for annotations based
    /// on who created them (Inspector, Verifier, or AI).
    /// </summary>
    public class AuthorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AuthorType author)
            {
                switch (author)
                {
                    case AuthorType.Inspector:
                        return Brushes.DodgerBlue; // Inspector is Blue
                    case AuthorType.Verifier:
                        return Brushes.Yellow;     // Verifier is Yellow
                    case AuthorType.AI:
                        return Brushes.Red;        // AI is Red
                    default:
                        return Brushes.White;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter only works one way, so we don't implement ConvertBack.
            throw new NotImplementedException();
        }
    }
}