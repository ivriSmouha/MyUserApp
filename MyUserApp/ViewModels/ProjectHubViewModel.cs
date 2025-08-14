// File: MyUserApp/ViewModels/ProjectHubViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class ProjectHubViewModel : BaseViewModel
    {
        private const string DefaultImagePath = "pack://application:,,,/Assets/placeholder.png";

        public string WelcomeMessage { get; }

        // ===================================================================
        // ==     CHANGE: The collection now holds our new display ViewModel    ==
        // ===================================================================
        public ObservableCollection<ProjectDisplayViewModel> RecentProjects { get; }

        private int _currentImageIndex;
        private string _previewImagePath;
        public string PreviewImagePath { get => _previewImagePath; private set { _previewImagePath = value; OnPropertyChanged(); } }
        private string _imageCounterText;
        public string ImageCounterText { get => _imageCounterText; private set { _imageCounterText = value; OnPropertyChanged(); } }

        private ProjectDisplayViewModel _selectedProject;
        public ProjectDisplayViewModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged();
                _currentImageIndex = 0;
                UpdateImageDisplay();
                ((RelayCommand)OpenProjectCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand OpenProjectCommand { get; }
        public event Action<InspectionReportModel, AuthorType> OnOpenProjectRequested;

        public ICommand StartNewProjectCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand NextImageCommand { get; }
        public ICommand PreviousImageCommand { get; }
        public event Action OnLogoutRequested;
        public event Action<UserModel> OnStartNewProjectRequested;
        private readonly UserModel _currentUser;

        public ProjectHubViewModel(UserModel user)
        {
            _currentUser = user;
            WelcomeMessage = $"Welcome, {user.Username}!";

            // ===================================================================
            // ==     CHANGE: Convert the raw data models into our new ViewModel  ==
            // ===================================================================
            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);
            // We use LINQ's 'Select' to transform each report into a ProjectDisplayViewModel,
            // passing in the user so it can determine their role.
            RecentProjects = new ObservableCollection<ProjectDisplayViewModel>(
                userProjects.Select(report => new ProjectDisplayViewModel(report, user))
            );
            // ===================================================================

            NextImageCommand = new RelayCommand(ShowNextImage, _ => CanShowNextImage());
            PreviousImageCommand = new RelayCommand(ShowPreviousImage, _ => CanShowPreviousImage());
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            OpenProjectCommand = new RelayCommand(OpenSelectedProject, _ => SelectedProject != null);

            UpdateImageDisplay();
        }

        private void OpenSelectedProject(object obj)
        {
            if (SelectedProject != null)
            {
                // ===================================================================
                // ==    CHANGE: We now pass both the report and the determined role   ==
                // ===================================================================
                OnOpenProjectRequested?.Invoke(SelectedProject.Report, SelectedProject.CurrentUserRole);
            }
        }

        private void ShowNextImage(object obj) { _currentImageIndex++; UpdateImageDisplay(); }
        private bool CanShowNextImage() => _selectedProject != null && _selectedProject.Report.ImagePaths.Count > 1 && _currentImageIndex < _selectedProject.Report.ImagePaths.Count - 1;
        private void ShowPreviousImage(object obj) { _currentImageIndex--; UpdateImageDisplay(); }
        private bool CanShowPreviousImage() => _selectedProject != null && _currentImageIndex > 0;

        private void UpdateImageDisplay()
        {
            if (_selectedProject != null && _selectedProject.Report.ImagePaths.Any())
            {
                PreviewImagePath = _selectedProject.Report.ImagePaths[_currentImageIndex];
                ImageCounterText = $"Image {_currentImageIndex + 1} of {_selectedProject.Report.ImagePaths.Count}";
            }
            else
            {
                PreviewImagePath = DefaultImagePath;
                ImageCounterText = "No Images";
            }

            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PreviousImageCommand).RaiseCanExecuteChanged();
        }
    }
}