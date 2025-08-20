// File: MyUserApp/Converters/PathToBitmapImageConverter.cs
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MyUserApp.Converters
{
    /// <summary>
    /// Converts a file path string into a BitmapImage object that does NOT lock the underlying file.
    /// This is critical for allowing files to be deleted while they are displayed in the UI.
    /// </summary>
    public class PathToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Get the full path from the binding.
            string path = value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            // Create a new BitmapImage object.
            BitmapImage image = new BitmapImage();

            // Use BeginInit/EndInit to control the loading process.
            image.BeginInit();

            // Set the URI source of the image.
            image.UriSource = new Uri(path);

            // IMPORTANT: This is the magic line.
            // It tells WPF to load the image fully into memory at creation time.
            // Once loaded, the file on disk is no longer needed and the lock is released.
            image.CacheOption = BitmapCacheOption.OnLoad;

            // Finalize the initialization.
            image.EndInit();

            // Freeze the image for performance benefits, as it won't be changed.
            image.Freeze();

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter only works one way.
            throw new NotImplementedException();
        }
    }
}