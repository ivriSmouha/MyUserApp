// File: MyUserApp/ViewModels/MainViewModel.cs

using MyUserApp.Models;
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
                if (_currentView is ImageEditorViewModel oldEditorVM)
                {
                    oldEditorVM.SaveAnnotations();
                }
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private readonly AdminPanelViewModel _adminPanelVM;
        private UserModel _currentUser;

        public MainViewModel()
        {
            _adminPanelVM = new AdminPanelViewModel();
            _adminPanelVM.OnLogoutRequested += ShowLoginView;
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

            // ===================================================================
            // ==    CHANGE: The event now provides the role, so we pass it on  ==
            // ===================================================================
            projectHubVM.OnOpenProjectRequested += async (report, role) => await ShowImageEditorAsync(report, user, role);

            CurrentView = projectHubVM;
        }

        private void ShowReportEntryScreen(UserModel user)
        {
            var reportEntryVM = new ReportEntryViewModel(user);
            reportEntryVM.OnCancelled += () => ShowProjectHub(user);
            // When creating a NEW report, the user is always the Inspector.
            reportEntryVM.OnReportSubmitted += async (report) => await OnReportSubmissionSucceededAsync(report, user);
            reportEntryVM.OnLogoutRequested += ShowLoginView;
            CurrentView = reportEntryVM;
        }

        private async Task OnReportSubmissionSucceededAsync(InspectionReportModel report, UserModel user)
        {
            // For a new report, the creator's role is always Inspector.
            await ShowImageEditorAsync(report, user, AuthorType.Inspector);
        }

        // ===================================================================
        // ==    CHANGE: Method signature updated to accept the user's role   ==
        // ===================================================================
        private async Task ShowImageEditorAsync(InspectionReportModel report, UserModel user, AuthorType role)
        {
            // Pass the role to the editor's constructor.
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