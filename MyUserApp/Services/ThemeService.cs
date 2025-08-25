// File: MyUserApp/Services/ThemeService.cs
using System;
using System.Linq;
using System.Windows;

namespace MyUserApp.Services
{
    public class ThemeService
    {
        private static readonly Lazy<ThemeService> _instance = new Lazy<ThemeService>(() => new ThemeService());
        public static ThemeService Instance => _instance.Value;

        private bool _isLightTheme = false; // Start with dark theme

        public void SwitchTheme()
        {
            _isLightTheme = !_isLightTheme;

            var existingTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme"));

            if (existingTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
            }

            var newTheme = new ResourceDictionary
            {
                Source = _isLightTheme
                    ? new Uri("Themes/LightTheme.xaml", UriKind.Relative)
                    : new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(newTheme);
        }
    }
}