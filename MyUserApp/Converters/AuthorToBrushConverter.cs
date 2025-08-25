using MyUserApp.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A value converter that transforms an AuthorType enum into a specific Brush color.
    /// This is used in the UI to color-code annotations based on their author.
    /// </summary>
    public class AuthorToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts an AuthorType to a Brush.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AuthorType author)
            {
                switch (author)
                {
                    case AuthorType.Inspector:
                        return Brushes.DodgerBlue;
                    case AuthorType.Verifier:
                        return Brushes.Yellow;
                    case AuthorType.AI:
                        return Brushes.Red;
                    default:
                        return Brushes.White; // Default color
                }
            }
            // Return a transparent brush if the input is not a valid AuthorType.
            return Brushes.Transparent;
        }

        /// <summary>
        /// Converts a Brush back to an AuthorType. This is not implemented as it's not needed.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}