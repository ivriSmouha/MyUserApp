using MyUserApp.Models;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Linq; // Added for .Any() check

namespace MyUserApp.Services
{
    /// <summary>
    /// A singleton service to manage loading, saving, and modifying
    /// application-wide options like dropdown lists.
    /// </summary>
    public class OptionsService
    {
        private const string FilePath = "app_options.json";

        // This is the standard singleton pattern. It ensures only one instance
        // of this service ever exists.
        private static readonly OptionsService _instance = new OptionsService();
        public static OptionsService Instance => _instance;

        public AppOptionsModel Options { get; private set; }

        // The constructor is private to enforce the singleton pattern.
        // It loads the options from the file as soon as the service is created.
        private OptionsService()
        {
            LoadOptions();
        }

        public void LoadOptions()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = File.ReadAllText(FilePath);
                    // Safely deserialize. If the file is corrupt or empty, it will return null.
                    var loadedOptions = JsonSerializer.Deserialize<AppOptionsModel>(json);

                    // If loading fails or the file is empty, create new default options.
                    if (loadedOptions == null)
                    {
                        CreateDefaultOptions();
                        return;
                    }

                    // Re-wrap collections in new ObservableCollections to ensure they are valid.
                    Options = new AppOptionsModel
                    {
                        AircraftTypes = new ObservableCollection<string>(loadedOptions.AircraftTypes ?? Enumerable.Empty<string>()),
                        TailNumbers = new ObservableCollection<string>(loadedOptions.TailNumbers ?? Enumerable.Empty<string>()),
                        AircraftSides = new ObservableCollection<string>(loadedOptions.AircraftSides ?? Enumerable.Empty<string>()),
                        Reasons = new ObservableCollection<string>(loadedOptions.Reasons ?? Enumerable.Empty<string>())
                    };
                }
                catch // Catch potential file access or JSON format errors
                {
                    CreateDefaultOptions();
                }
            }
            else
            {
                CreateDefaultOptions();
            }
        }

        private void CreateDefaultOptions()
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

        public void SaveOptions()
        {
            var json = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        #region Methods for Modifying Options

        // --- Aircraft Types ---
        public void AddAircraftType(string type)
        {
            if (!string.IsNullOrWhiteSpace(type) && !Options.AircraftTypes.Contains(type))
            {
                Options.AircraftTypes.Add(type);
                SaveOptions();
            }
        }
        public void DeleteAircraftType(string type)
        {
            if (Options.AircraftTypes.Remove(type))
            {
                SaveOptions();
            }
        }

        // --- Tail Numbers ---
        public void AddTailNumber(string tailNumber)
        {
            if (!string.IsNullOrWhiteSpace(tailNumber) && !Options.TailNumbers.Contains(tailNumber))
            {
                Options.TailNumbers.Add(tailNumber);
                SaveOptions();
            }
        }
        public void DeleteTailNumber(string tailNumber)
        {
            if (Options.TailNumbers.Remove(tailNumber))
            {
                SaveOptions();
            }
        }

        // --- Aircraft Sides ---
        public void AddAircraftSide(string side)
        {
            if (!string.IsNullOrWhiteSpace(side) && !Options.AircraftSides.Contains(side))
            {
                Options.AircraftSides.Add(side);
                SaveOptions();
            }
        }
        public void DeleteAircraftSide(string side)
        {
            if (Options.AircraftSides.Remove(side))
            {
                SaveOptions();
            }
        }

        // --- Reasons ---
        public void AddReason(string reason)
        {
            if (!string.IsNullOrWhiteSpace(reason) && !Options.Reasons.Contains(reason))
            {
                Options.Reasons.Add(reason);
                SaveOptions();
            }
        }
        public void DeleteReason(string reason)
        {
            if (Options.Reasons.Remove(reason))
            {
                SaveOptions();
            }
        }

        #endregion
    }
}