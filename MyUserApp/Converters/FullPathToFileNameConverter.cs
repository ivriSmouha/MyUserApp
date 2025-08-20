// File: MyUserApp/Converters/FullPathToFileNameConverter.cs
using System;
using System.Globalization;
using System.IO; // Required for Path.GetFileName
using System.Windows.Data;

namespace MyUserApp.Converters
{
    /// <summary>
    /// Converts a full file path string into just the file name for display purposes.
    /// For example: "C:\Users\...\ReportImages\guid\image.jpg" becomes "image.jpg".
    /// </summary>
    public class FullPathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the incoming value is a valid string.
            if (value is string fullPath && !string.IsNullOrEmpty(fullPath))
            {
                try
                {
                    // Use the built-in System.IO.Path class to safely get the file name.
                    return Path.GetFileName(fullPath);
                }
                catch (ArgumentException)
                {
                    // If the path contains invalid characters, return it as is.
                    return fullPath;
                }
            }

            // If the value is not a string or is empty, return an empty string.
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter only works one way (from full path to file name).
            throw new NotImplementedException();
        }
    }
}