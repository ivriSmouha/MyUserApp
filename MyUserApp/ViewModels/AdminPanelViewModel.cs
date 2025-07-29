// ViewModels/AdminPanelViewModel.cs
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
    public class AdminPanelViewModel : BaseViewModel
    {
        // --- User management properties (no changes) ---
        private string _newUsername;
        public string NewUsername { get => _newUsername; set { _newUsername = value; OnPropertyChanged(); } }
        private string _newPassword;
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(); } }
        private bool _newUserIsAdmin;
        public bool NewUserIsAdmin { get => _newUserIsAdmin; set { _newUserIsAdmin = value; OnPropertyChanged(); } }
        public ObservableCollection<UserModel> Users { get; set; }

        // --- Options management properties ---
        public AppOptionsModel AppOptions => OptionsService.Instance.Options;
        private string _newAircraftType;
        public string NewAircraftType { get => _newAircraftType; set { _newAircraftType = value; OnPropertyChanged(); } }
        // --- NEW: Properties for other options ---
        private string _newAircraftSide;
        public string NewAircraftSide { get => _newAircraftSide; set { _newAircraftSide = value; OnPropertyChanged(); } }
        private string _newReason;
        public string NewReason { get => _newReason; set { _newReason = value; OnPropertyChanged(); } }

        // --- Commands ---
        public ICommand AddUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        // --- NEW: Commands for other options ---
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }

        public event Action OnLogoutRequested;
        private readonly string _userFilePath = "users.json";

        public AdminPanelViewModel()
        {
            // User management setup (no changes)
            LoadUsers();
            AddUserCommand = new RelayCommand(AddNewUser, CanAddNewUser);
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // Options management setup
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);
            // --- NEW: Initialize new commands ---
            AddAircraftSideCommand = new RelayCommand(AddAircraftSide, _ => !string.IsNullOrEmpty(NewAircraftSide));
            DeleteAircraftSideCommand = new RelayCommand(DeleteAircraftSide);
            AddReasonCommand = new RelayCommand(AddReason, _ => !string.IsNullOrEmpty(NewReason));
            DeleteReasonCommand = new RelayCommand(DeleteReason);
        }

        // --- Options management logic ---
        private void AddAircraftType(object obj)
        {
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

        // --- NEW: Logic for other options (repeating the pattern) ---
        private void AddAircraftSide(object obj)
        {
            OptionsService.Instance.Options.AircraftSides.Add(NewAircraftSide);
            OptionsService.Instance.SaveOptions();
            NewAircraftSide = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        private void DeleteAircraftSide(object obj)
        {
            if (obj is string sideToDelete)
            {
                OptionsService.Instance.Options.AircraftSides.Remove(sideToDelete);
                OptionsService.Instance.SaveOptions();
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        private void AddReason(object obj)
        {
            OptionsService.Instance.Options.Reasons.Add(NewReason);
            OptionsService.Instance.SaveOptions();
            NewReason = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        private void DeleteReason(object obj)
        {
            if (obj is string reasonToDelete)
            {
                OptionsService.Instance.Options.Reasons.Remove(reasonToDelete);
                OptionsService.Instance.SaveOptions();
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        // --- User management logic (no changes) ---
        // ... (Methods like AddNewUser, LoadUsers, etc. remain here)
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