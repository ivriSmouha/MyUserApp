// ViewModels/MainViewModel.cs
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
            var projectHubVM = new ProjectHubViewModel(user);
            projectHubVM.OnLogoutRequested += ShowLoginView;
            projectHubVM.OnStartNewProjectRequested += ShowReportEntryScreen;
            CurrentView = projectHubVM;
        }

        private void ShowReportEntryScreen(UserModel user)
        {
            var reportEntryVM = new ReportEntryViewModel(user);
            // This line handles the "Submit" or "Cancel" action.
            reportEntryVM.OnFinished += () => ShowProjectHub(user);

            // --- THIS IS THE FIX ---
            // This line listens for the logout request and navigates back to the login screen.
            reportEntryVM.OnLogoutRequested += ShowLoginView;

            CurrentView = reportEntryVM;
        }

        private UserModel AuthenticateUser(string username, string password)
        {
            return _adminPanelVM.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}