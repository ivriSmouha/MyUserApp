using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Admin Panel view. It handles all administrative logic,
    /// including user management and management of dropdown options.
    /// </summary>
    public class AdminPanelViewModel : BaseViewModel
    {
        #region User Management Properties
        /// <summary>
        /// A view collection of users that supports filtering.
        /// </summary>
        public ICollectionView UsersView { get; }

        /// <summary>
        /// Text used to filter the list of users.
        /// </summary>
        private string _userFilterText;
        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                _userFilterText = value;
                OnPropertyChanged();
                UsersView.Refresh(); // Refresh the filter when text changes.
            }
        }

        /// <summary>
        /// The username for a new user being created.
        /// </summary>
        private string _newUsername;
        public string NewUsername { get => _newUsername; set { _newUsername = value; OnPropertyChanged(); ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged(); } }

        /// <summary>
        /// The password for a new user being created.
        /// </summary>
        private string _newPassword;
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(); ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged(); } }

        /// <summary>
        /// Flag indicating if the new user should be an admin.
        /// </summary>
        private bool _newUserIsAdmin;
        public bool NewUserIsAdmin { get => _newUserIsAdmin; set { _newUserIsAdmin = value; OnPropertyChanged(); } }
        #endregion

        #region Options Management Properties
        /// <summary>
        /// Provides access to the application's global options for binding.
        /// </summary>
        public AppOptionsModel AppOptions => OptionsService.Instance.Options;

        /// <summary>
        /// The name of a new aircraft type to add.
        /// </summary>
        private string _newAircraftType;
        public string NewAircraftType { get => _newAircraftType; set { _newAircraftType = value; OnPropertyChanged(); ((RelayCommand)AddAircraftTypeCommand).RaiseCanExecuteChanged(); } }

        /// <summary>
        /// The name of a new aircraft side to add.
        /// </summary>
        private string _newAircraftSide;
        public string NewAircraftSide { get => _newAircraftSide; set { _newAircraftSide = value; OnPropertyChanged(); ((RelayCommand)AddAircraftSideCommand).RaiseCanExecuteChanged(); } }

        /// <summary>
        /// The name of a new inspection reason to add.
        /// </summary>
        private string _newReason;
        public string NewReason { get => _newReason; set { _newReason = value; OnPropertyChanged(); ((RelayCommand)AddReasonCommand).RaiseCanExecuteChanged(); } }

        /// <summary>
        /// A new tail number to add.
        /// </summary>
        private string _newTailNumber;
        public string NewTailNumber { get => _newTailNumber; set { _newTailNumber = value; OnPropertyChanged(); ((RelayCommand)AddTailNumberCommand).RaiseCanExecuteChanged(); } }
        #endregion

        #region Commands
        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SwitchThemeCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }
        public ICommand AddTailNumberCommand { get; }
        public ICommand DeleteTailNumberCommand { get; }
        #endregion

        /// <summary>
        /// Event triggered to request a logout action from the main view.
        /// </summary>
        public event Action OnLogoutRequested;

        /// <summary>
        /// Initializes a new instance of the AdminPanelViewModel.
        /// Sets up data views and commands.
        /// </summary>
        public AdminPanelViewModel()
        {
            // Set up the user list view and its filter.
            UsersView = CollectionViewSource.GetDefaultView(UserService.Instance.Users);
            UsersView.Filter = FilterUsers;

            // Initialize all commands for user and options management.
            AddUserCommand = new RelayCommand(AddNewUser, _ => CanAddNewUser());
            DeleteUserCommand = new RelayCommand(DeleteUser);
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            SwitchThemeCommand = new RelayCommand(_ => ThemeService.Instance.SwitchTheme());

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
        /// <summary>
        /// Filter logic for the UsersView. Returns true if a user should be visible.
        /// </summary>
        private bool FilterUsers(object item)
        {
            if (string.IsNullOrWhiteSpace(UserFilterText))
            {
                return true; // No filter text, so show all users.
            }
            if (item is UserModel user)
            {
                // Show user if their username contains the filter text (case-insensitive).
                return user.Username.Contains(UserFilterText, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Determines if the AddUserCommand can be executed.
        /// </summary>
        private bool CanAddNewUser() => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);

        /// <summary>
        /// Executes the logic to add a new user.
        /// </summary>
        private void AddNewUser(object obj)
        {
            var newUser = new UserModel { Username = this.NewUsername, Password = this.NewPassword, IsAdmin = this.NewUserIsAdmin };
            UserService.Instance.AddUser(newUser);
            // Clear input fields after adding the user.
            NewUsername = "";
            NewPassword = "";
            NewUserIsAdmin = false;
        }

        /// <summary>
        /// Executes the logic to delete a selected user after confirmation.
        /// </summary>
        private void DeleteUser(object obj)
        {
            if (obj is UserModel userToDelete)
            {
                // Get project counts to inform the admin of the impact.
                var (inspectorCount, verifierCount) = ReportService.Instance.GetProjectCountsForUser(userToDelete.Username);
                string message = $"Are you sure you want to delete the user '{userToDelete.Username}'?\n\n" +
                                 $"This will reassign all of their projects to the '{UserService.DeletedUserUsername}' account.\n" +
                                 $"Projects affected: {inspectorCount} as Inspector, {verifierCount} as Verifier.\n\n" +
                                 "This action cannot be undone.";

                // Show a confirmation dialog before proceeding.
                if (MessageBox.Show(message, "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    UserService.Instance.DeleteUserAndReassignProjects(userToDelete);
                }
            }
        }
        #endregion

        #region Options Management Methods
        /// <summary>
        /// Adds a new aircraft type to the global options.
        /// </summary>
        private void AddAircraftType(object obj)
        {
            OptionsService.Instance.AddAircraftType(NewAircraftType);
            NewAircraftType = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        /// <summary>
        /// Deletes an aircraft type from the global options.
        /// </summary>
        private void DeleteAircraftType(object obj)
        {
            if (obj is string s)
            {
                OptionsService.Instance.DeleteAircraftType(s);
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        /// <summary>
        /// Adds a new aircraft side to the global options.
        /// </summary>
        private void AddAircraftSide(object obj)
        {
            OptionsService.Instance.AddAircraftSide(NewAircraftSide);
            NewAircraftSide = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        /// <summary>
        /// Deletes an aircraft side from the global options.
        /// </summary>
        private void DeleteAircraftSide(object obj)
        {
            if (obj is string s)
            {
                OptionsService.Instance.DeleteAircraftSide(s);
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        /// <summary>
        /// Adds a new inspection reason to the global options.
        /// </summary>
        private void AddReason(object obj)
        {
            OptionsService.Instance.AddReason(NewReason);
            NewReason = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        /// <summary>
        /// Deletes an inspection reason from the global options.
        /// </summary>
        private void DeleteReason(object obj)
        {
            if (obj is string s)
            {
                OptionsService.Instance.DeleteReason(s);
                OnPropertyChanged(nameof(AppOptions));
            }
        }

        /// <summary>
        /// Adds a new tail number to the global options.
        /// </summary>
        private void AddTailNumber(object obj)
        {
            OptionsService.Instance.AddTailNumber(NewTailNumber);
            NewTailNumber = "";
            OnPropertyChanged(nameof(AppOptions));
        }

        /// <summary>
        /// Deletes a tail number from the global options.
        /// </summary>
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