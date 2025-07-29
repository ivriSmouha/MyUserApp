using MyUserApp.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace MyUserApp.Services
{
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
                var loadedOptions = JsonSerializer.Deserialize<AppOptionsModel>(json);

                Options = new AppOptionsModel
                {
                    AircraftTypes = new ObservableCollection<string>(loadedOptions.AircraftTypes),
                    AircraftSides = new ObservableCollection<string>(loadedOptions.AircraftSides),
                    Reasons = new ObservableCollection<string>(loadedOptions.Reasons),
                    // THE FIX: Handle if TailNumbers is null in an old file, by providing an empty list.
                    TailNumbers = new ObservableCollection<string>(loadedOptions.TailNumbers ?? new List<string>())
                };
            }
            else
            {
                Options = new AppOptionsModel
                {
                    AircraftTypes = { "Boeing 737", "Airbus A320" },
                    AircraftSides = { "Left", "Right", "Top", "Bottom" },
                    Reasons = { "Routine Check", "Reported Issue" },
                    TailNumbers = { "N123UA", "N456SW", "N789DL" }
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