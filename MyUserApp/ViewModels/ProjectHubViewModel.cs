// ViewModels/ProjectHubViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class ProjectHubViewModel : BaseViewModel
    {
        // --- Existing Properties ---
        public string WelcomeMessage { get; }
        public ObservableCollection<InspectionReportModel> RecentProjects { get; }

        // --- THIS IS THE FIX ---
        // A property to store the report that is currently selected in the ListView.
        private InspectionReportModel _selectedProject;
        public InspectionReportModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                // This notifies the UI that the selection has changed, so the image preview should update.
                OnPropertyChanged();
            }
        }

        // --- Commands and Events (no changes) ---
        public ICommand StartNewProjectCommand { get; }
        public ICommand LogoutCommand { get; }
        public event Action OnLogoutRequested;
        public event Action<UserModel> OnStartNewProjectRequested;
        private readonly UserModel _currentUser;

        public ProjectHubViewModel(UserModel user)
        {
            _currentUser = user;
            WelcomeMessage = $"Welcome, {user.Username}!";
            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);
            RecentProjects = new ObservableCollection<InspectionReportModel>(userProjects);
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
        }
    }
}