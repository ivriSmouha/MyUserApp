using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    // This ViewModel controls the entire admin screen, including its tabs.
    public class AdminPanelViewModel : BaseViewModel
    {
        // User management properties
        private string _newUsername;
        public string NewUsername { get => _newUsername; set { _newUsername = value; OnPropertyChanged(); } }
        private string _newPassword;
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(); } }
        private bool _newUserIsAdmin;
        public bool NewUserIsAdmin { get => _newUserIsAdmin; set { _newUserIsAdmin = value; OnPropertyChanged(); } }
        public ObservableCollection<UserModel> Users { get; set; }

        // Options management properties
        public AppOptionsModel AppOptions => OptionsService.Instance.Options;
        private string _newAircraftType;
        public string NewAircraftType { get => _newAircraftType; set { _newAircraftType = value; OnPropertyChanged(); } }

        // Commands
        public ICommand AddUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }

        // Events
        public event Action OnLogoutRequested;
        private readonly string _userFilePath = "users.json";

        public AdminPanelViewModel()
        {
            // User management setup
            LoadUsers();
            AddUserCommand = new RelayCommand(AddNewUser, CanAddNewUser);
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // Options management setup
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);
        }

        // ... (All AddUser, LoadUser, etc. methods from previous steps go here)
        private void AddAircraftType(object obj)
        {
            if (string.IsNullOrWhiteSpace(NewAircraftType)) return;
            OptionsService.Instance.Options.AircraftTypes.Add(NewAircraftType);
            OptionsService.Instance.SaveOptions();
            NewAircraftType = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        private void DeleteAircraftType(object obj)
        {
            if (obj is string typeToDelete)
            {
                OptionsService.Instance.Options.AircraftTypes.Remove(typeToDelete);
                OptionsService.Instance.SaveOptions();
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        // --- User management logic (unchanged) ---
        private void AddNewUser(object obj)
        {
            if (Users.Any(u => u.Username == NewUsername))
            {
                System.Windows.MessageBox.Show("Username already exists.");
                return;
            }
            var newUser = new UserModel { Username = NewUsername, Password = NewPassword, IsAdmin = NewUserIsAdmin };
            Users.Add(newUser);
            SaveUsers();
            NewUsername = "";
            NewPassword = "";
            NewUserIsAdmin = false;
        }
        private bool CanAddNewUser(object obj) => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);
        private void LoadUsers()
        {
            if (File.Exists(_userFilePath))
            {
                var json = File.ReadAllText(_userFilePath);
                Users = JsonSerializer.Deserialize<ObservableCollection<UserModel>>(json);
            }
            else
            {
                Users = new ObservableCollection<UserModel> { new UserModel { Username = "admin", Password = "123", IsAdmin = true } };
            }
        }
        private void SaveUsers()
        {
            var json = JsonSerializer.Serialize(Users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_userFilePath, json);
        }
    }
}