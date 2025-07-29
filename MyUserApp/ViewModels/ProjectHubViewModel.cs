using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    // The ViewModel for the non-admin user's home screen.
    public class ProjectHubViewModel : BaseViewModel
    {
        public string WelcomeMessage { get; }
        public ObservableCollection<InspectionReportModel> RecentProjects { get; }
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