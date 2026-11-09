// File: MyUserApp/Converters/PathToBitmapImageConverter.cs
using FellowOakDicom;
using FellowOakDicom.Imaging;
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
 
            string path = value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            string ext = Path.GetExtension(path).ToLower();

            // אם מדובר בקובץ DICOM
            if (ext == ".dcm" || ext == ".dicom")
            {
                try
                {
                    var file = DicomFile.Open(path);
                    var dicomImage = new DicomImage(file.Dataset);
                    IImage image = dicomImage.RenderImage();

                    byte[] pixels = image.AsBytes();

                    // יצירת WriteableBitmap ישירות מתוך בייטים של הפיקסלים (ללא תלות ב-System.Drawing.Bitmap)
                    var writeableBmp = new WriteableBitmap(
                        image.Width,
                        image.Height,
                        96, 96,
                        System.Windows.Media.PixelFormats.Bgra32,
                        null);

                    writeableBmp.WritePixels(
                        new System.Windows.Int32Rect(0, 0, image.Width, image.Height),
                        pixels,
                        image.Width * 4,
                        0);

                    writeableBmp.Freeze();
                    return writeableBmp;
                }
                catch
                {
                    return null; // מונע קריסה במקרה של קובץ פגום
                }
            }
            else
            {
                // עבור תמונות רגילות (JPG, PNG וכו')
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch
                {
                    return null;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            throw new NotImplementedException();
        }
    }
}