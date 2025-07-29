using MyUserApp.Models;
using System.IO;
using System.Text.Json;

namespace MyUserApp.Services
{
    // Singleton service to manage application-wide dropdown options.
    public class OptionsService
    {
        private const string FilePath = "app_options.json";
        private static readonly OptionsService _instance = new OptionsService();
        public static OptionsService Instance => _instance;

        public AppOptionsModel Options { get; private set; }

        private OptionsService() { LoadOptions(); }

        public void LoadOptions()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Options = JsonSerializer.Deserialize<AppOptionsModel>(json);
            }
            else
            {
                Options = new AppOptionsModel
                {
                    AircraftTypes = { "Boeing 737", "Airbus A320" },
                    AircraftSides = { "Left", "Right", "Top", "Bottom" },
                    Reasons = { "Routine Check", "Reported Issue" }
                };
                SaveOptions();
            }
        }

        public void SaveOptions()
        {
            var json = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}