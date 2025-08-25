using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// The main ViewModel for the application. It acts as a controller,
    /// managing which view is currently displayed (e.g., Login, Project Hub, Editor).
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        /// <summary>
        /// The currently active ViewModel, which is bound to the main window's content.
        /// </summary>
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

        // Stores the currently logged-in user.
        private UserModel _currentUser;

        /// <summary>
        /// Initializes the MainViewModel and displays the login view.
        /// </summary>
        public MainViewModel()
        {
            ShowLoginView();
        }

        /// <summary>
        /// Navigates to the Login view.
        /// </summary>
        private void ShowLoginView()
        {
            _currentUser = null;
            var loginVM = new LoginViewModel(AuthenticateUser);
            loginVM.OnLoginSuccess += OnLoginSucceeded;
            CurrentView = loginVM;
        }

        /// <summary>
        /// Callback method executed upon a successful login. Navigates to the
        /// appropriate view based on the user's role (Admin or standard user).
        /// </summary>
        /// <param name="user">The successfully authenticated user.</param>
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

        /// <summary>
        /// Navigates to the Project Hub view for a given user.
        /// </summary>
        /// <param name="user">The current user.</param>
        private void ShowProjectHub(UserModel user)
        {
            if (user == null) { ShowLoginView(); return; }
            var projectHubVM = new ProjectHubViewModel(user);
            projectHubVM.OnLogoutRequested += ShowLoginView;
            projectHubVM.OnStartNewProjectRequested += ShowReportEntryScreen;
            projectHubVM.OnOpenProjectRequested += async (report, role) => await ShowImageEditorAsync(report, user, role);
            CurrentView = projectHubVM;
        }

        /// <summary>
        /// Navigates to the screen for creating a new inspection report.
        /// </summary>
        /// <param name="user">The user creating the report.</param>
        private void ShowReportEntryScreen(UserModel user)
        {
            var reportEntryVM = new ReportEntryViewModel(user);
            reportEntryVM.OnCancelled += () => ShowProjectHub(user);
            reportEntryVM.OnReportSubmitted += async (report) => await OnReportSubmissionSucceededAsync(report, user);
            reportEntryVM.OnLogoutRequested += ShowLoginView;
            CurrentView = reportEntryVM;
        }

        /// <summary>
        /// Callback executed after a new report is created. It automatically
        /// navigates to the Image Editor for the new report.
        /// </summary>
        private async Task OnReportSubmissionSucceededAsync(InspectionReportModel report, UserModel user)
        {
            await ShowImageEditorAsync(report, user, AuthorType.Inspector);
        }

        /// <summary>
        /// Navigates to the Image Editor view for a specific report.
        /// </summary>
        /// <param name="report">The report to be edited.</param>
        /// <param name="user">The current user.</param>
        /// <param name="role">The role of the user for this specific report.</param>
        private async Task ShowImageEditorAsync(InspectionReportModel report, UserModel user, AuthorType role)
        {
            var imageEditorVM = new ImageEditorViewModel(report, user, role);
            imageEditorVM.OnFinished += () => ShowProjectHub(user);
            CurrentView = imageEditorVM;
            await imageEditorVM.InitializeAsync();
        }

        /// <summary>
        /// Performs user authentication by checking credentials.
        /// </summary>
        /// <param name="username">The username to authenticate.</param>
        /// <param name="password">The password to authenticate.</param>
        /// <returns>The UserModel if authentication is successful; otherwise, null.</returns>
        private UserModel AuthenticateUser(string username, string password)
        {
            return Services.UserService.Instance.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}