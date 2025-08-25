using System;
using System.Globalization;
using System.IO; // Required for Path operations
using System.Windows.Data;

namespace MyUserApp.Converters
{
    /// <summary>
    /// A value converter that extracts just the file name from a full file path string.
    /// Example: "C:\folder\image.jpg" becomes "image.jpg".
    /// </summary>
    public class FullPathToFileNameConverter : IValueConverter
    {
        /// <summary>
        /// Converts a full path string to just the file name.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure the input value is a non-empty string.
            if (value is string fullPath && !string.IsNullOrEmpty(fullPath))
            {
                try
                {
                    // Use the robust Path.GetFileName method to extract the file name.
                    return Path.GetFileName(fullPath);
                }
                catch (ArgumentException)
                {
                    // In case of an invalid path, return the original string.
                    return fullPath;
                }
            }
            // Return empty for null or empty input.
            return string.Empty;
        }

        /// <summary>
        /// Converts a file name back to a full path. This is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}