// File: MyUserApp/ViewModels/ProjectHubViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MyUserApp.ViewModels
{
    public class ProjectHubViewModel : BaseViewModel
    {
        private const string DefaultImagePath = "pack://application:,,,/Assets/placeholder.png";
        private readonly ObservableCollection<ProjectDisplayViewModel> _recentProjects;

        public ICollectionView ProjectsView { get; }

        public string WelcomeMessage { get; }

        private int _currentImageIndex;

        private BitmapSource _previewImageSource;
        public BitmapSource PreviewImageSource { get => _previewImageSource; private set { _previewImageSource = value; OnPropertyChanged(); } }

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

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                ProjectsView.Refresh(); // Trigger the filter
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

            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);
            _recentProjects = new ObservableCollection<ProjectDisplayViewModel>(
                userProjects.Select(report => new ProjectDisplayViewModel(report, user))
            );

            ProjectsView = CollectionViewSource.GetDefaultView(_recentProjects);
            ProjectsView.SortDescriptions.Add(new SortDescription(nameof(ProjectDisplayViewModel.LastModifiedDate), ListSortDirection.Descending));
            ProjectsView.Filter = FilterProjects;


            NextImageCommand = new RelayCommand(ShowNextImage, _ => CanShowNextImage());
            PreviousImageCommand = new RelayCommand(ShowPreviousImage, _ => CanShowPreviousImage());
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            OpenProjectCommand = new RelayCommand(OpenSelectedProject, _ => SelectedProject != null);

            UpdateImageDisplay();
        }

        private bool FilterProjects(object item)
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                return true; // No filter, show all items
            }

            if (item is ProjectDisplayViewModel project)
            {
                var filterText = FilterText.Trim();
                // Check multiple fields for the filter text
                return (project.ProjectName?.Contains(filterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (project.InspectorName?.Contains(filterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (project.VerifierName?.Contains(filterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (project.RoleDisplayString?.Contains(filterText, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            return false;
        }


        private void OpenSelectedProject(object obj)
        {
            if (SelectedProject != null)
            {
                OnOpenProjectRequested?.Invoke(SelectedProject.Report, SelectedProject.CurrentUserRole);
            }
        }

        private void ShowNextImage(object obj) { _currentImageIndex++; UpdateImageDisplay(); }
        private bool CanShowNextImage() => _selectedProject != null && _selectedProject.Report.ImagePaths.Count > 1 && _currentImageIndex < _selectedProject.Report.ImagePaths.Count - 1;
        private void ShowPreviousImage(object obj) { _currentImageIndex--; UpdateImageDisplay(); }
        private bool CanShowPreviousImage() => _selectedProject != null && _currentImageIndex > 0;

        private async void UpdateImageDisplay()
        {
            BitmapSource imageToShow = null;

            if (_selectedProject != null && _selectedProject.Report.ImagePaths.Any())
            {
                ImageCounterText = $"Image {_currentImageIndex + 1} of {_selectedProject.Report.ImagePaths.Count}";
                string imagePath = _selectedProject.Report.ImagePaths[_currentImageIndex];

                // Asynchronously create a downscaled thumbnail to prevent UI lag
                imageToShow = await Task.Run(() =>
                {
                    if (!File.Exists(imagePath)) return null;

                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imagePath);
                        // This is the key optimization: only decode the image to a small size
                        bitmap.DecodePixelWidth = 300;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Fully load into memory
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make the image thread-safe
                        return bitmap;
                    }
                    catch
                    {
                        // Handle potential file corruption or access errors
                        return null;
                    }
                });
            }

            if (imageToShow == null)
            {
                // If loading failed or there are no images, use the default placeholder
                imageToShow = new BitmapImage(new Uri(DefaultImagePath));
                ImageCounterText = "No Images";
            }

            PreviewImageSource = imageToShow;

            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PreviousImageCommand).RaiseCanExecuteChanged();
        }
    }
}