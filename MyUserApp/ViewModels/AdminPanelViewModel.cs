using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
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
        public ICollectionView UsersView { get; }

        private string _userFilterText;
        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                _userFilterText = value;
                OnPropertyChanged();
                UsersView.Refresh(); // Trigger the filter to re-evaluate
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
                return true; // No filter text, show all users.
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

            // Reset fields
            NewUsername = "";
            NewPassword = "";
            NewUserIsAdmin = false;
        }

        private void DeleteUser(object obj)
        {
            if (obj is UserModel userToDelete)
            {
                // Step 1: Get the counts for the confirmation message without changing data.
                var (inspectorCount, verifierCount) = ReportService.Instance.GetProjectCountsForUser(userToDelete.Username);

                string message = $"Are you sure you want to delete the user '{userToDelete.Username}'?\n\n" +
                                 $"This will reassign all of their projects to the '{UserService.DeletedUserUsername}' account.\n" +
                                 $"Projects affected: {inspectorCount} as Inspector, {verifierCount} as Verifier.\n\n" +
                                 "This action cannot be undone.";

                // Step 2: Show the confirmation box.
                if (MessageBox.Show(message, "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // Step 3: If confirmed, execute the deletion and reassignment.
                    UserService.Instance.DeleteUserAndReassignProjects(userToDelete);
                }
            }
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