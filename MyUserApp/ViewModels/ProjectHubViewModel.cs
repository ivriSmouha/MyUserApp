// ViewModels/ProjectHubViewModel.cs
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
        // --- NEW: A constant holding the special path to our project resource ---
        // This is the "Pack URI" format for WPF resources.
        private const string DefaultImagePath = "pack://application:,,,/Assets/placeholder.png";

        // --- Existing Properties ---
        public string WelcomeMessage { get; }
        public ObservableCollection<InspectionReportModel> RecentProjects { get; }
        private int _currentImageIndex;
        private string _previewImagePath;
        public string PreviewImagePath { get => _previewImagePath; private set { _previewImagePath = value; OnPropertyChanged(); } }
        private string _imageCounterText;
        public string ImageCounterText { get => _imageCounterText; private set { _imageCounterText = value; OnPropertyChanged(); } }

        private InspectionReportModel _selectedProject;
        public InspectionReportModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged();
                _currentImageIndex = 0;
                UpdateImageDisplay();
            }
        }

        // --- Commands and Events (no changes) ---
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
            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);
            RecentProjects = new ObservableCollection<InspectionReportModel>(userProjects);

            NextImageCommand = new RelayCommand(ShowNextImage, _ => CanShowNextImage());
            PreviousImageCommand = new RelayCommand(ShowPreviousImage, _ => CanShowPreviousImage());
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // Initialize the display with the default image.
            UpdateImageDisplay();
        }

        private void ShowNextImage(object obj) { _currentImageIndex++; UpdateImageDisplay(); }
        private bool CanShowNextImage() => _selectedProject != null && _currentImageIndex < _selectedProject.ImagePaths.Count - 1;
        private void ShowPreviousImage(object obj) { _currentImageIndex--; UpdateImageDisplay(); }
        private bool CanShowPreviousImage() => _selectedProject != null && _currentImageIndex > 0;

        // --- THIS IS THE CORE LOGIC CHANGE ---
        private void UpdateImageDisplay()
        {
            // Check if a project is selected AND if that project has any images.
            if (_selectedProject != null && _selectedProject.ImagePaths.Any())
            {
                // If yes, show the real image and the counter.
                PreviewImagePath = _selectedProject.ImagePaths[_currentImageIndex];
                ImageCounterText = $"Image {_currentImageIndex + 1} of {_selectedProject.ImagePaths.Count}";
            }
            else
            {
                PreviewImagePath = "pack://application:,,,/Assets/placeholder.png";
                ImageCounterText = "No Images";
            }
        }
    }
}