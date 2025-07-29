// Services/OptionsService.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

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
                    // --- NEW: Handle loading Tail Numbers ---
                    TailNumbers = new ObservableCollection<string>(loadedOptions.TailNumbers),
                    AircraftSides = new ObservableCollection<string>(loadedOptions.AircraftSides),
                    Reasons = new ObservableCollection<string>(loadedOptions.Reasons)
                };
            }
            else
            {
                // If no file exists, create a default set of options.
                Options = new AppOptionsModel
                {
                    AircraftTypes = { "Boeing 737", "Airbus A320" },
                    // --- NEW: Add default Tail Numbers ---
                    TailNumbers = { "N12345", "N54321", "N98765" },
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
}```

---

### Step 3: Update the Admin Panel (`AdminPanelViewModel` and `AdminPanelView`)

We will add the complete UI and logic for the admin to manage tail numbers.

#### **File to Modify: `ViewModels/AdminPanelViewModel.cs`**

Here we add the properties and commands to handle the new option.

```csharp
// ViewModels/AdminPanelViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class AdminPanelViewModel : BaseViewModel
    {
        // --- User management properties (no changes) ---
        public ObservableCollection<UserModel> Users => UserService.Instance.Users;
        private string _newUsername;
        public string NewUsername { get => _newUsername; set { _newUsername = value; OnPropertyChanged(); } }
        private string _newPassword;
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(); } }
        private bool _newUserIsAdmin;
        public bool NewUserIsAdmin { get => _newUserIsAdmin; set { _newUserIsAdmin = value; OnPropertyChanged(); } }

        // --- Options management properties ---
        public AppOptionsModel AppOptions => OptionsService.Instance.Options;
        private string _newAircraftType;
        public string NewAircraftType { get => _newAircraftType; set { _newAircraftType = value; OnPropertyChanged(); } }
        private string _newAircraftSide;
        public string NewAircraftSide { get => _newAircraftSide; set { _newAircraftSide = value; OnPropertyChanged(); } }
        private string _newReason;
        public string NewReason { get => _newReason; set { _newReason = value; OnPropertyChanged(); } }
        // --- NEW: Property for adding a new tail number ---
        private string _newTailNumber;
        public string NewTailNumber { get => _newTailNumber; set { _newTailNumber = value; OnPropertyChanged(); } }

        // --- Commands ---
        public ICommand AddUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }
        // --- NEW: Commands for tail numbers ---
        public ICommand AddTailNumberCommand { get; }
        public ICommand DeleteTailNumberCommand { get; }

        public event Action OnLogoutRequested;

        public AdminPanelViewModel()
        {
            // Wire up all commands
            AddUserCommand = new RelayCommand(AddNewUser, _ => CanAddNewUser());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);
            AddAircraftSideCommand = new RelayCommand(AddAircraftSide, _ => !string.IsNullOrEmpty(NewAircraftSide));
            DeleteAircraftSideCommand = new RelayCommand(DeleteAircraftSide);
            AddReasonCommand = new RelayCommand(AddReason, _ => !string.IsNullOrEmpty(NewReason));
            DeleteReasonCommand = new RelayCommand(DeleteReason);
            // --- NEW: Initialize new commands ---
            AddTailNumberCommand = new RelayCommand(AddTailNumber, _ => !string.IsNullOrEmpty(NewTailNumber));
            DeleteTailNumberCommand = new RelayCommand(DeleteTailNumber);
        }

        private bool CanAddNewUser() => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);
        private void AddNewUser(object obj)
        {
            var newUser = new UserModel { Username = this.NewUsername, Password = this.NewPassword, IsAdmin = this.NewUserIsAdmin };
            UserService.Instance.AddUser(newUser);
            NewUsername = ""; NewPassword = ""; NewUserIsAdmin = false;
        }

        // --- NEW: Logic for tail numbers ---
        private void AddTailNumber(object obj)
        { OptionsService.Instance.Options.TailNumbers.Add(NewTailNumber); OptionsService.Instance.SaveOptions(); NewTailNumber = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteTailNumber(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.TailNumbers.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }

        // --- Other options logic (no changes) ---
        private void AddAircraftType(object obj)
        { OptionsService.Instance.Options.AircraftTypes.Add(NewAircraftType); OptionsService.Instance.SaveOptions(); NewAircraftType = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteAircraftType(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.AircraftTypes.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
        private void AddAircraftSide(object obj)
        { OptionsService.Instance.Options.AircraftSides.Add(NewAircraftSide); OptionsService.Instance.SaveOptions(); NewAircraftSide = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteAircraftSide(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.AircraftSides.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
        private void AddReason(object obj)
        { OptionsService.Instance.Options.Reasons.Add(NewReason); OptionsService.Instance.SaveOptions(); NewReason = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteReason(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.Reasons.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
    }
}