using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Admin Panel. Manages users and application-wide options 
    /// such as aircraft types, tail numbers, sides, and reasons.
    /// </summary>
    public class AdminPanelViewModel : BaseViewModel
    {
        // --- User Management Properties ---
        public ObservableCollection<UserModel> Users => UserService.Instance.Users;

        private string _newUsername;
        public string NewUsername { get => _newUsername; set { _newUsername = value; OnPropertyChanged(); ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged(); } }

        private string _newPassword;
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(); ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged(); } }

        private bool _newUserIsAdmin;
        public bool NewUserIsAdmin { get => _newUserIsAdmin; set { _newUserIsAdmin = value; OnPropertyChanged(); } }

        // --- Options Management Properties ---
        public AppOptionsModel AppOptions => OptionsService.Instance.Options;

        private string _newAircraftType;
        public string NewAircraftType { get => _newAircraftType; set { _newAircraftType = value; OnPropertyChanged(); ((RelayCommand)AddAircraftTypeCommand).RaiseCanExecuteChanged(); } }

        private string _newAircraftSide;
        public string NewAircraftSide { get => _newAircraftSide; set { _newAircraftSide = value; OnPropertyChanged(); ((RelayCommand)AddAircraftSideCommand).RaiseCanExecuteChanged(); } }

        private string _newReason;
        public string NewReason { get => _newReason; set { _newReason = value; OnPropertyChanged(); ((RelayCommand)AddReasonCommand).RaiseCanExecuteChanged(); } }

        // --- ADDED: Property for Tail Number input to fix binding error ---
        private string _newTailNumber;
        public string NewTailNumber { get => _newTailNumber; set { _newTailNumber = value; OnPropertyChanged(); ((RelayCommand)AddTailNumberCommand).RaiseCanExecuteChanged(); } }


        // --- Commands ---
        public ICommand AddUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }

        // --- ADDED: Commands for Tail Number buttons to fix binding errors ---
        public ICommand AddTailNumberCommand { get; }
        public ICommand DeleteTailNumberCommand { get; }


        // --- Events ---
        public event Action OnLogoutRequested;


        public AdminPanelViewModel()
        {
            // Initialize User commands
            AddUserCommand = new RelayCommand(AddNewUser, _ => CanAddNewUser());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // Initialize Options commands
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);

            AddAircraftSideCommand = new RelayCommand(AddAircraftSide, _ => !string.IsNullOrEmpty(NewAircraftSide));
            DeleteAircraftSideCommand = new RelayCommand(DeleteAircraftSide);

            AddReasonCommand = new RelayCommand(AddReason, _ => !string.IsNullOrEmpty(NewReason));
            DeleteReasonCommand = new RelayCommand(DeleteReason);

            // --- ADDED: Initialize the new Tail Number commands ---
            AddTailNumberCommand = new RelayCommand(AddTailNumber, _ => !string.IsNullOrEmpty(NewTailNumber));
            DeleteTailNumberCommand = new RelayCommand(DeleteTailNumber);
        }

        #region User Management Methods
        private bool CanAddNewUser() => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);

        private void AddNewUser(object obj)
        {
            var newUser = new UserModel { Username = this.NewUsername, Password = this.NewPassword, IsAdmin = this.NewUserIsAdmin };
            UserService.Instance.AddUser(newUser);

            // Reset fields
            NewUsername = "";
            NewPassword = "";
            NewUserIsAdmin = false;
        }
        #endregion

        #region Options Management Methods

        // Aircraft Types
        private void AddAircraftType(object obj)
        {
            OptionsService.Instance.AddAircraftType(NewAircraftType);
            NewAircraftType = "";
            OnPropertyChanged(nameof(AppOptions));
        }
        private void DeleteAircraftType(object obj)
        {
            if (obj is string s)
            {
                OptionsService.Instance.DeleteAircraftType(s);
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        // Aircraft Sides
        private void AddAircraftSide(object obj)
        {
            OptionsService.Instance.AddAircraftSide(NewAircraftSide);
            NewAircraftSide = "";
            OnPropertyChanged(nameof(AppOptions));
        }
        private void DeleteAircraftSide(object obj)
        {
            if (obj is string s)
            {
                OptionsService.Instance.DeleteAircraftSide(s);
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        // Reasons
        private void AddReason(object obj)
        {
            OptionsService.Instance.AddReason(NewReason);
            NewReason = "";
            OnPropertyChanged(nameof(AppOptions));
        }
        private void DeleteReason(object obj)
        {
            if (obj is string s)
            {
                OptionsService.Instance.DeleteReason(s);
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        // --- ADDED: Methods for adding and deleting Tail Numbers ---
        private void AddTailNumber(object obj)
        {
            OptionsService.Instance.AddTailNumber(NewTailNumber);
            NewTailNumber = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        private void DeleteTailNumber(object obj)
        {
            if (obj is string tailNumberToDelete)
            {
                OptionsService.Instance.DeleteTailNumber(tailNumberToDelete);
                OnPropertyChanged(nameof(AppOptions));
            }
        }
        #endregion
    }
}