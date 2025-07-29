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
        // --- Existing Properties ---
        public string WelcomeMessage { get; }
        public ObservableCollection<InspectionReportModel> RecentProjects { get; }

        // --- NEW: A private field to track the current image index ---
        private int _currentImageIndex;

        // --- Properties for Image Carousel ---
        private string _previewImagePath;
        public string PreviewImagePath
        {
            get => _previewImagePath;
            private set { _previewImagePath = value; OnPropertyChanged(); }
        }

        private string _imageCounterText;
        public string ImageCounterText
        {
            get => _imageCounterText;
            private set { _imageCounterText = value; OnPropertyChanged(); }
        }

        // --- Modified SelectedProject Property ---
        private InspectionReportModel _selectedProject;
        public InspectionReportModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged(); // Notify UI about selection change.

                // When a new project is selected, reset the image viewer to the first image.
                _currentImageIndex = 0;
                UpdateImageDisplay();
            }
        }

        // --- Commands ---
        public ICommand StartNewProjectCommand { get; }
        public ICommand LogoutCommand { get; }
        // --- NEW: Commands for the image carousel ---
        public ICommand NextImageCommand { get; }
        public ICommand PreviousImageCommand { get; }

        // --- Events ---
        public event Action OnLogoutRequested;
        public event Action<UserModel> OnStartNewProjectRequested;
        private readonly UserModel _currentUser;

        public ProjectHubViewModel(UserModel user)
        {
            _currentUser = user;
            WelcomeMessage = $"Welcome, {user.Username}!";
            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);
            RecentProjects = new ObservableCollection<InspectionReportModel>(userProjects);

            // Wire up commands
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            // --- NEW: Initialize carousel commands ---
            // The CanExecute part is crucial: it disables the buttons when at the start or end of the list.
            NextImageCommand = new RelayCommand(ShowNextImage, _ => CanShowNextImage());
            PreviousImageCommand = new RelayCommand(ShowPreviousImage, _ => CanShowPreviousImage());

            // Initialize the display
            UpdateImageDisplay();
        }

        // --- NEW: Helper Methods for Carousel Logic ---

        private void ShowNextImage(object obj)
        {
            _currentImageIndex++;
            UpdateImageDisplay();
        }

        private bool CanShowNextImage()
        {
            // We can show the next image only if a project is selected AND we are not on the last image.
            return _selectedProject != null && _currentImageIndex < _selectedProject.ImagePaths.Count - 1;
        }

        private void ShowPreviousImage(object obj)
        {
            _currentImageIndex--;
            UpdateImageDisplay();
        }



        private bool CanShowPreviousImage()
        {
            // We can show the previous image only if we are not on the first image.
            return _selectedProject != null && _currentImageIndex > 0;
        }

        // This central method updates both the image and the counter text.
        private void UpdateImageDisplay()
        {
            if (_selectedProject != null && _selectedProject.ImagePaths.Any())
            {
                // If there's a selected project with images, update the path and counter.
                PreviewImagePath = _selectedProject.ImagePaths[_currentImageIndex];
                ImageCounterText = $"Image {_currentImageIndex + 1} of {_selectedProject.ImagePaths.Count}";
            }
            else
            {
                // **THE CRASH FIX:** If no images, set path to empty and update text.
                // An empty string is handled gracefully by the Image control.
                PreviewImagePath = string.Empty;
                ImageCounterText = "No Images";
            }
        }
    }
}