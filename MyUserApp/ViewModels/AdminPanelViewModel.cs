// File: MyUserApp/ViewModels/AdminPanelViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class AdminPanelViewModel : BaseViewModel
    {
        // --- User Management Properties ---
        public ICollectionView UsersView { get; }

        private string _userFilterText;
        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                _userFilterText = value;
                OnPropertyChanged();
                UsersView.Refresh();
            }
        }

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

        private string _newTailNumber;
        public string NewTailNumber { get => _newTailNumber; set { _newTailNumber = value; OnPropertyChanged(); ((RelayCommand)AddTailNumberCommand).RaiseCanExecuteChanged(); } }

        // --- Commands ---
        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SwitchThemeCommand { get; } // ADDED THIS LINE
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }
        public ICommand AddTailNumberCommand { get; }
        public ICommand DeleteTailNumberCommand { get; }

        // --- Events ---
        public event Action OnLogoutRequested;

        public AdminPanelViewModel()
        {
            // --- User Management ---
            UsersView = CollectionViewSource.GetDefaultView(UserService.Instance.Users);
            UsersView.Filter = FilterUsers;

            AddUserCommand = new RelayCommand(AddNewUser, _ => CanAddNewUser());
            DeleteUserCommand = new RelayCommand(DeleteUser);
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // ADDED THIS LINE: Initialize the command to call the simple ThemeService.
            SwitchThemeCommand = new RelayCommand(_ => ThemeService.Instance.SwitchTheme());

            // --- Options Management ---
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);
            AddAircraftSideCommand = new RelayCommand(AddAircraftSide, _ => !string.IsNullOrEmpty(NewAircraftSide));
            DeleteAircraftSideCommand = new RelayCommand(DeleteAircraftSide);
            AddReasonCommand = new RelayCommand(AddReason, _ => !string.IsNullOrEmpty(NewReason));
            DeleteReasonCommand = new RelayCommand(DeleteReason);
            AddTailNumberCommand = new RelayCommand(AddTailNumber, _ => !string.IsNullOrEmpty(NewTailNumber));
            DeleteTailNumberCommand = new RelayCommand(DeleteTailNumber);
        }

        #region User Management Methods
        private bool FilterUsers(object item)
        {
            if (string.IsNullOrWhiteSpace(UserFilterText))
            {
                return true;
            }
            if (item is UserModel user)
            {
                return user.Username.Contains(UserFilterText, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private bool CanAddNewUser() => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);

        private void AddNewUser(object obj)
        {
            var newUser = new UserModel { Username = this.NewUsername, Password = this.NewPassword, IsAdmin = this.NewUserIsAdmin };
            UserService.Instance.AddUser(newUser);
            NewUsername = "";
            NewPassword = "";
            NewUserIsAdmin = false;
        }

        private void DeleteUser(object obj)
        {
            if (obj is UserModel userToDelete)
            {
                var (inspectorCount, verifierCount) = ReportService.Instance.GetProjectCountsForUser(userToDelete.Username);
                string message = $"Are you sure you want to delete the user '{userToDelete.Username}'?\n\n" +
                                 $"This will reassign all of their projects to the '{UserService.DeletedUserUsername}' account.\n" +
                                 $"Projects affected: {inspectorCount} as Inspector, {verifierCount} as Verifier.\n\n" +
                                 "This action cannot be undone.";
                if (MessageBox.Show(message, "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    UserService.Instance.DeleteUserAndReassignProjects(userToDelete);
                }
            }
        }
        #endregion

        #region Options Management Methods
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