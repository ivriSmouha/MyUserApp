// File: MyUserApp/ViewModels/MainViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyUserApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentView;
        public BaseViewModel CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private UserModel _currentUser;

        public MainViewModel()
        {
            ShowLoginView();
        }

        private void ShowLoginView()
        {
            _currentUser = null;
            var loginVM = new LoginViewModel(AuthenticateUser);
            loginVM.OnLoginSuccess += OnLoginSucceeded;
            CurrentView = loginVM;
        }

        private void OnLoginSucceeded(UserModel user)
        {
            _currentUser = user;
            if (user.IsAdmin)
            {
                var adminPanelVM = new AdminPanelViewModel();
                adminPanelVM.OnLogoutRequested += ShowLoginView;
                CurrentView = adminPanelVM;
            }
            else
            {
                ShowProjectHub(user);
            }
        }

        private void ShowProjectHub(UserModel user)
        {
            if (user == null) { ShowLoginView(); return; }
            var projectHubVM = new ProjectHubViewModel(user);
            projectHubVM.OnLogoutRequested += ShowLoginView;
            projectHubVM.OnStartNewProjectRequested += ShowReportEntryScreen;
            projectHubVM.OnOpenProjectRequested += async (report, role) => await ShowImageEditorAsync(report, user, role);
            CurrentView = projectHubVM;
        }

        private void ShowReportEntryScreen(UserModel user)
        {
            var reportEntryVM = new ReportEntryViewModel(user);
            reportEntryVM.OnCancelled += () => ShowProjectHub(user);
            reportEntryVM.OnReportSubmitted += async (report) => await OnReportSubmissionSucceededAsync(report, user);
            reportEntryVM.OnLogoutRequested += ShowLoginView;
            CurrentView = reportEntryVM;
        }

        private async Task OnReportSubmissionSucceededAsync(InspectionReportModel report, UserModel user)
        {
            await ShowImageEditorAsync(report, user, AuthorType.Inspector);
        }

        private async Task ShowImageEditorAsync(InspectionReportModel report, UserModel user, AuthorType role)
        {
            var imageEditorVM = new ImageEditorViewModel(report, user, role);
            imageEditorVM.OnFinished += () => ShowProjectHub(user);
            CurrentView = imageEditorVM;
            await imageEditorVM.InitializeAsync();
        }

        private UserModel AuthenticateUser(string username, string password)
        {
            return Services.UserService.Instance.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}