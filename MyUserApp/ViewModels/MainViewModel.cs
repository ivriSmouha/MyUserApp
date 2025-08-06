// File: MyUserApp/ViewModels/MainViewModel.cs

using MyUserApp.Models;
using System.Linq;

namespace MyUserApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentView;
        public BaseViewModel CurrentView { get => _currentView; set { _currentView = value; OnPropertyChanged(); } }

        private readonly AdminPanelViewModel _adminPanelVM;

        public MainViewModel()
        {
            _adminPanelVM = new AdminPanelViewModel();
            _adminPanelVM.OnLogoutRequested += ShowLoginView;
            ShowLoginView();
        }

        private void ShowLoginView()
        {
            var loginVM = new LoginViewModel(AuthenticateUser);
            loginVM.OnLoginSuccess += OnLoginSucceeded;
            CurrentView = loginVM;
        }

        private void OnLoginSucceeded(UserModel user)
        {
            if (user.IsAdmin)
            {
                CurrentView = _adminPanelVM;
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
            CurrentView = projectHubVM;
        }

        private void ShowReportEntryScreen(UserModel user)
        {
            var reportEntryVM = new ReportEntryViewModel(user);

            reportEntryVM.OnCancelled += () => ShowProjectHub(user);
            reportEntryVM.OnReportSubmitted += (report) => OnReportSubmissionSucceeded(report, user);
            reportEntryVM.OnLogoutRequested += ShowLoginView;

            CurrentView = reportEntryVM;
        }

        // --- THIS IS THE FIX ---
        // This method now correctly calls ShowImageEditor to navigate to the next screen.
        private void OnReportSubmissionSucceeded(InspectionReportModel report, UserModel user)
        {
            ShowImageEditor(report, user);
        }

        private void ShowImageEditor(InspectionReportModel report, UserModel user)
        {
            var imageEditorVM = new ImageEditorViewModel(report, user);
            imageEditorVM.OnFinished += () => ShowProjectHub(user);
            CurrentView = imageEditorVM;
        }

        private UserModel AuthenticateUser(string username, string password)
        {
            // Note: It is better practice to get users from the UserService directly.
            // This will work, but consider changing it for consistency.
            return Services.UserService.Instance.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}