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
    /// <summary>
    /// ViewModel for the Project Hub view. This screen displays a list of projects
    /// assigned to the current user and allows them to open, filter, or create new projects.
    /// </summary>
    public class ProjectHubViewModel : BaseViewModel
    {
        // A placeholder image URI used when no project image is available.
        private const string DefaultImagePath = "pack://application:,,,/Assets/placeholder.png";

        // The underlying collection of projects.
        private readonly ObservableCollection<ProjectDisplayViewModel> _recentProjects;

        /// <summary>
        /// A view of the project collection that supports filtering and sorting.
        /// </summary>
        public ICollectionView ProjectsView { get; }

        /// <summary>
        /// A welcome message personalized for the logged-in user.
        /// </summary>
        public string WelcomeMessage { get; }

        // State for the image previewer.
        private int _currentImageIndex;
        private BitmapSource _previewImageSource;
        public BitmapSource PreviewImageSource { get => _previewImageSource; private set { _previewImageSource = value; OnPropertyChanged(); } }

        private string _imageCounterText;
        public string ImageCounterText { get => _imageCounterText; private set { _imageCounterText = value; OnPropertyChanged(); } }

        /// <summary>
        /// The currently selected project in the list.
        /// </summary>
        private ProjectDisplayViewModel _selectedProject;
        public ProjectDisplayViewModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged();
                _currentImageIndex = 0; // Reset image index on new selection.
                UpdateImageDisplay();
                ((RelayCommand)OpenProjectCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// The text used to filter the project list.
        /// </summary>
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                ProjectsView.Refresh();
            }
        }

        // Commands for UI actions.
        public ICommand OpenProjectCommand { get; }
        public ICommand StartNewProjectCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SwitchThemeCommand { get; }
        public ICommand NextImageCommand { get; }
        public ICommand PreviousImageCommand { get; }

        // Events to signal navigation requests to the MainViewModel.
        public event Action<InspectionReportModel, AuthorType> OnOpenProjectRequested;
        public event Action OnLogoutRequested;
        public event Action<UserModel> OnStartNewProjectRequested;

        private readonly UserModel _currentUser;

        /// <summary>
        /// Initializes the Project Hub, loading projects for the specified user.
        /// </summary>
        public ProjectHubViewModel(UserModel user)
        {
            _currentUser = user;
            WelcomeMessage = $"Welcome, {user.Username}!";

            // Fetch reports and wrap them in ProjectDisplayViewModels.
            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);
            _recentProjects = new ObservableCollection<ProjectDisplayViewModel>(
                userProjects.Select(report => new ProjectDisplayViewModel(report, user))
            );

            // Set up the collection view with sorting and filtering.
            ProjectsView = CollectionViewSource.GetDefaultView(_recentProjects);
            ProjectsView.SortDescriptions.Add(new SortDescription(nameof(ProjectDisplayViewModel.LastModifiedDate), ListSortDirection.Descending));
            ProjectsView.Filter = FilterProjects;

            // Initialize commands.
            NextImageCommand = new RelayCommand(ShowNextImage, _ => CanShowNextImage());
            PreviousImageCommand = new RelayCommand(ShowPreviousImage, _ => CanShowPreviousImage());
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            OpenProjectCommand = new RelayCommand(OpenSelectedProject, _ => SelectedProject != null);
            SwitchThemeCommand = new RelayCommand(_ => ThemeService.Instance.SwitchTheme());

            UpdateImageDisplay();
        }

        /// <summary>
        /// The filtering logic applied to the ProjectsView.
        /// </summary>
        private bool FilterProjects(object item)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;

            if (item is ProjectDisplayViewModel project)
            {
                var filter = FilterText.Trim();
                // Check if the filter text appears in any key project fields.
                return (project.ProjectName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (project.InspectorName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (project.VerifierName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (project.RoleDisplayString?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            return false;
        }

        /// <summary>
        /// Triggers the event to open the selected project in the editor.
        /// </summary>
        private void OpenSelectedProject(object obj)
        {
            if (SelectedProject != null)
            {
                OnOpenProjectRequested?.Invoke(SelectedProject.Report, SelectedProject.CurrentUserRole);
            }
        }

        // Methods for image preview navigation.
        private void ShowNextImage(object obj) { _currentImageIndex++; UpdateImageDisplay(); }
        private bool CanShowNextImage() => _selectedProject != null && _selectedProject.Report.ImagePaths.Count > 1 && _currentImageIndex < _selectedProject.Report.ImagePaths.Count - 1;
        private void ShowPreviousImage(object obj) { _currentImageIndex--; UpdateImageDisplay(); }
        private bool CanShowPreviousImage() => _selectedProject != null && _currentImageIndex > 0;

        /// <summary>
        /// Asynchronously loads and displays the current preview image for the selected project.
        /// </summary>
        private async void UpdateImageDisplay()
        {
            BitmapSource imageToShow = null;
            if (_selectedProject != null && _selectedProject.Report.ImagePaths.Any())
            {
                ImageCounterText = $"Image {_currentImageIndex + 1} of {_selectedProject.Report.ImagePaths.Count}";
                string imagePath = _selectedProject.Report.ImagePaths[_currentImageIndex];

                // Load the image on a background thread to keep the UI responsive.
                imageToShow = await Task.Run(() =>
                {
                    if (!File.Exists(imagePath)) return null;
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imagePath);
                        bitmap.DecodePixelWidth = 300; // Load a smaller version for performance.
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make it thread-safe.
                        return bitmap;
                    }
                    catch { return null; } // Handle potential file errors.
                });
            }

            // If no image could be loaded, use the default placeholder.
            if (imageToShow == null)
            {
                imageToShow = new BitmapImage(new Uri(DefaultImagePath));
                ImageCounterText = "No Images";
            }
            PreviewImageSource = imageToShow;

            // Update the state of the navigation buttons.
            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PreviousImageCommand).RaiseCanExecuteChanged();
        }
    }
}