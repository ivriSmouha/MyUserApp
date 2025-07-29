using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    // This ViewModel controls the entire admin screen, including its tabs.
    public class AdminPanelViewModel : BaseViewModel
    {
        // --- User management properties ---
        // The list of users now comes directly from the UserService.
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

        // --- Commands ---
        public ICommand AddUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }

        public event Action OnLogoutRequested;

        public AdminPanelViewModel()
        {
            // User commands
            AddUserCommand = new RelayCommand(AddNewUser, _ => CanAddNewUser());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // Options commands
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);
            AddAircraftSideCommand = new RelayCommand(AddAircraftSide, _ => !string.IsNullOrEmpty(NewAircraftSide));
            DeleteAircraftSideCommand = new RelayCommand(DeleteAircraftSide);
            AddReasonCommand = new RelayCommand(AddReason, _ => !string.IsNullOrEmpty(NewReason));
            DeleteReasonCommand = new RelayCommand(DeleteReason);
        }

        private void AddNewUser(object obj)
        {
            var newUser = new UserModel
            {
                Username = this.NewUsername,
                Password = this.NewPassword,
                IsAdmin = this.NewUserIsAdmin
            };
            UserService.Instance.AddUser(newUser);

            // Reset the fields
            NewUsername = "";
            NewPassword = "";
            NewUserIsAdmin = false;
        }
        private bool CanAddNewUser() => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);

        // --- Options management logic ---
        private void AddAircraftType(object obj)
        {
            OptionsService.Instance.Options.AircraftTypes.Add(NewAircraftType); OptionsService.Instance.SaveOptions(); NewAircraftType = ""; OnPropertyChanged(nameof(AppOptions));
        }
        private void DeleteAircraftType(object obj)
        {
            if (obj is string typeToDelete) { OptionsService.Instance.Options.AircraftTypes.Remove(typeToDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); }
        }
        private void AddAircraftSide(object obj)
        {
            OptionsService.Instance.Options.AircraftSides.Add(NewAircraftSide); OptionsService.Instance.SaveOptions(); NewAircraftSide = ""; OnPropertyChanged(nameof(AppOptions));
        }
        private void DeleteAircraftSide(object obj)
        {
            if (obj is string sideToDelete) { OptionsService.Instance.Options.AircraftSides.Remove(sideToDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); }
        }
        private void AddReason(object obj)
        {
            OptionsService.Instance.Options.Reasons.Add(NewReason); OptionsService.Instance.SaveOptions(); NewReason = ""; OnPropertyChanged(nameof(AppOptions));
        }
        private void DeleteReason(object obj)
        {
            if (obj is string reasonToDelete) { OptionsService.Instance.Options.Reasons.Remove(reasonToDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); }
        }
    }
}