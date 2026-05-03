using ClosedXML.Excel; // לעריכת האקסל
using Microsoft.Win32;
using MyUserApp.Models;
using MyUserApp.Services;
using Spire.Xls;       // להמרה ל-PDF
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
        private const string DefaultImagePath = "C:\\Users\\User\\Source\\Repos\\MyUserApp\\MyUserApp\\Assets\\1.PNG";
       
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
                _currentImageIndex = 0;
                // Reset image index on new selection.
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

        public ICommand GenerateCombinedReportCommand { get; }
        public ICommand CreateNewFolderCommand { get; }
        public ICommand SelectProjectCommand { get; }
        public ICommand StartEditCommand { get; }
        public ICommand EndEditCommand { get; }


        public ICommand OpenProjectCommand { get; }
        public ICommand StartNewProjectCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SwitchThemeCommand { get; }
        public ICommand NextImageCommand { get; }
        public ICommand PreviousImageCommand { get; }
        public ICommand DeleteProjectCommand { get; }

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

            // 1. טעינת הדוחות מהשירות
            var userProjects = ReportService.Instance.GetReportsForUser(user.Username);

            // 2. המרה של כל הדוחות ל-ViewModels של פרויקטים
            var allProjectsList = userProjects.Select(report => new ProjectDisplayViewModel(report, user)).ToList();
           
            // 3. לוגיקת הקיבוץ לתקיות - זה מה שהיה חסר!
            var groups = allProjectsList
                .GroupBy(p => ExtractAircraftName(p.ProjectName)) // מקבץ לפי שם המטוס
                .Select(g => new AircraftFolder
                {
                    AircraftName = g.Key,
                    Projects = new ObservableCollection<ProjectDisplayViewModel>(g)
                });

            GroupedProjects = new ObservableCollection<AircraftFolder>(groups);
            _recentProjects = new ObservableCollection<ProjectDisplayViewModel>(allProjectsList);
            ProjectsView = CollectionViewSource.GetDefaultView(_recentProjects);
            ProjectsView.SortDescriptions.Add(new SortDescription(nameof(ProjectDisplayViewModel.LastModifiedDate), ListSortDirection.Descending));
            ProjectsView.Filter = FilterProjects;


            NextImageCommand = new RelayCommand(ShowNextImage, _ => CanShowNextImage());
            PreviousImageCommand = new RelayCommand(ShowPreviousImage, _ => CanShowPreviousImage());
            StartNewProjectCommand = new RelayCommand(param => OnStartNewProjectRequested?.Invoke(_currentUser));
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            OpenProjectCommand = new RelayCommand(OpenSelectedProject, _ => SelectedProject != null);
            SwitchThemeCommand = new RelayCommand(_ => ThemeService.Instance.SwitchTheme());
            DeleteProjectCommand = new RelayCommand(DeleteProject);
            SelectProjectCommand = new RelayCommand(obj =>
            {
                if (obj is ProjectDisplayViewModel project) SelectedProject = project;
            });
            GenerateCombinedReportCommand = new RelayCommand(GenerateCombinedReport);

            UpdateImageDisplay();
        }

        private readonly Dictionary<string, string> _imageCellMapping = new Dictionary<string, string>     
        {

        { "L7", "I13" },
        { "L8", "I14" },
        { "L9", "I15" },
        { "L10", "I16" },
        { "L11", "I17" },
        { "L11A", "I18" },
        { "L12", "I19" },
        { "L12A", "I20" },
        { "L13", "I21" },
        { "L14", "I22" },
        { "L15", "I23" },
        { "L16", "I24" },
        { "L17H", "I25" },
        { "L18H", "I26" },
        { "L17", "I27" },
        { "L18", "I28" },
        { "L19", "I29" },
        { "L20", "I30" },
        { "L21H", "I31" },
        { "L22H", "I32" },
        { "L21", "I33" },
        { "L22", "I34" },




        { "R7", "D13" },
        { "R8", "D14" },
        { "R9", "D15" },
        { "R10", "D16" },
        { "R11", "D17" },
        { "R11A", "D18" },
        { "R12", "D19" },
        { "R12A", "D20" },
        { "R13", "D21" },
        { "R14", "D22" },
        { "R15", "D23" },
        { "R16", "D24" },
        { "R17H", "D25" },
        { "R18H", "D26" },
        { "R17", "D27" },
        { "R18", "D28" },
        { "R19", "D29" },
        { "R20", "D30" },
        { "R21H", "D31" },
        { "R22H", "D32" },
        { "R21", "D33" },
        { "R22", "D34" }
        };

        private async void GenerateCombinedReport(object obj)
        {
            if (obj is not AircraftFolder folder) return;

            // 1. איתור הפרויקטים (ימין ושמאל)
            var rightProject = folder.Projects.FirstOrDefault(p => p.ProjectName.Contains("(R)", StringComparison.OrdinalIgnoreCase));
            var leftProject = folder.Projects.FirstOrDefault(p => p.ProjectName.Contains("(L)", StringComparison.OrdinalIgnoreCase));
            //var rightProject = folder.Projects.FirstOrDefault(p => p.ProjectName.Contains("(R)"));
            //var leftProject = folder.Projects.FirstOrDefault(p => p.ProjectName.Contains("(L)"));

            if (rightProject == null || leftProject == null)
            {
                MessageBox.Show("חסר פרויקט ימין (R) או שמאל (L) בתיקייה זו.", "שגיאה");
                return;
            }

            try
            {
                // 2. הגדרת נתיבים (בדיוק כמו בקוד שעובד לך)
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "מDEX_EXCEL_דוח גוז.xlsx");
                if (!File.Exists(templatePath))
                {
                    MessageBox.Show("קובץ תבנית האקסל לא נמצא!");
                    return;
                }
                   


                string tempExcelPath = Path.Combine(Path.GetTempPath(), $"CombinedReport_{Guid.NewGuid()}.xlsx");
                File.Copy(templatePath, tempExcelPath, true);

                using (var workbook = new XLWorkbook(tempExcelPath))
                {
                    var worksheet = workbook.Worksheet(1);

                    // פונקציית עזר פנימית לביצוע המילוי - חוסכת כתיבה כפולה לימין ושמאל
                    void FillExcelFromProject(ProjectDisplayViewModel project)
                    {
                        if (project?.Report?.ImageStatuses == null) return;

                        // רצים על כל הסטטוסים שיש בפרויקט
                        foreach (var statusEntry in project.Report.ImageStatuses)
                        {
                            // מחלצים את שם הקובץ בלבד מתוך המפתח (למקרה שהמפתח הוא נתיב מלא)
                            // והופכים לאותיות גדולות כדי למנוע כפילויות
                            string fileName = Path.GetFileNameWithoutExtension(statusEntry.Key).ToUpper();
                            string status = statusEntry.Value;

                            // בודקים אם שם הקובץ הזה קיים במיפוי לאקסל שלנו
                            // שימי לב: הפכתי גם את המפתח במילון המיפוי ל-ToUpper
                            var mappingKey = _imageCellMapping.Keys.FirstOrDefault(k => k.ToUpper() == fileName);

                            if (mappingKey != null && !string.IsNullOrEmpty(status))
                            {
                                string targetCell = _imageCellMapping[mappingKey];
                                worksheet.Cell(targetCell).Value = status;

                                // Debug קטן למקרה שזה עדיין לא עובד:
                                Debug.WriteLine($"Filled Cell {targetCell} with {status} for file {fileName}");
                            }
                        }
                    }

                    // א. מילוי נתונים מהפרויקט השמאלי
                    FillExcelFromProject(leftProject);

                    // ב. מילוי נתונים מהפרויקט הימני
                    FillExcelFromProject(rightProject);

                    workbook.Save();
                }

                // 4. פתיחת הקובץ לעריכה (בדיוק כמו בקוד שעובד)
                Process.Start(new ProcessStartInfo(tempExcelPath) { UseShellExecute = true });

                MessageBox.Show("הקובץ נפתח. בצעי שינויים, שמרי, ולחצי אישור לייצוא PDF.");

                // 5. שמירה ל-PDF (בדיוק כמו בקוד שעובד)
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"דוח משולב {folder.AircraftName}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    Spire.Xls.Workbook spireWorkbook = new Spire.Xls.Workbook();
                    spireWorkbook.LoadFromFile(tempExcelPath);
                    spireWorkbook.SaveToFile(saveFileDialog.FileName, Spire.Xls.FileFormat.PDF);

                    MessageBox.Show("הדוח נשמר בהצלחה!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה: {ex.Message}");
            }
        }

        private string ExtractAircraftName(string projectName)
        {
            if (string.IsNullOrEmpty(projectName)) return "General";
            // בדיקה אם קיים תו הכוכבית שקבענו כמפריד
            if (projectName.Contains("*"))
            {
                // פיצול לפי כוכבית ולקיחת החלק הראשון (שם המטוס)
                return projectName.Split('*')[0].Trim();
            }
            // fallback: אם אין כוכבית (עבור דוחות ישנים), ננסה מקף או מילה ראשונה
            if (projectName.Contains("-"))
            {
                return projectName.Split('-')[0].Trim();
            }
            return projectName.Split(' ')[0].Trim();
        }

        // מחלקה חדשה לייצוג תיקיית מטוס
        /*
        public class AircraftFolder
        {
            public string AircraftName { get; set; }
            public ObservableCollection<ProjectDisplayViewModel> Projects { get; set; } = new ObservableCollection<ProjectDisplayViewModel>();
        }*/
        public class AircraftFolder : BaseViewModel // הוספנו ירושה כדי שהמסך יתעדכן
        {
            private string _aircraftName;
            private bool _isEditing;
            public string AircraftName
            {
                get => _aircraftName;
                set
                {
                    _aircraftName = value;
                    OnPropertyChanged(); // מעדכן את המסך כשהשם משתנה
                }
            }
            public bool IsEditing
            {
                get => _isEditing;
                set
                {
                    _isEditing = value;
                    OnPropertyChanged(); // מעדכן את המסך כשנכנסים/יוצאים ממצב עריכה
                }
            }

            public ObservableCollection<ProjectDisplayViewModel> Projects { get; set; } = new ObservableCollection<ProjectDisplayViewModel>();
        }

        // בתוך ה-ProjectHubViewModel:
        private ObservableCollection<AircraftFolder> _groupedProjects;
        public ObservableCollection<AircraftFolder> GroupedProjects
        {
            get => _groupedProjects;
            set { _groupedProjects = value; OnPropertyChanged(); }
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
        private void DeleteProject(object obj)
        {
            if (obj is not ProjectDisplayViewModel project)
                return;

            var result = MessageBox.Show(
                $"Delete project '{project.ProjectName}' ?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            // 1. מחיקה מהמסד נתונים/שירות
            ReportService.Instance.DeleteReport(project.Report);

            // 2. עדכון הטבלה המרכזית (זה מה שהיה לך)
            _recentProjects.Remove(project);

            // 3. עדכון התיקיות בצד (זה החלק החסר)
            // אנחנו מחפשים באיזו תיקייה הפרויקט הזה נמצא ומסירים אותו ממנה
            var folderContainingProject = GroupedProjects.FirstOrDefault(f => f.Projects.Contains(project));
            if (folderContainingProject != null)
            {
                folderContainingProject.Projects.Remove(project);

                if (folderContainingProject.Projects.Count == 0)
                {
                    GroupedProjects.Remove(folderContainingProject);
                }
            }
        }

        private void CreateNewFolder(object obj)
        {
            // 1. יצירת אובייקט התיקייה החדש
            var newFolder = new AircraftFolder
            {
                AircraftName = "New Aircraft", // שם ברירת מחדל
                IsEditing = true,               // הפעלת מצב עריכה (זה יראה את ה-TextBox)
                Projects = new ObservableCollection<ProjectDisplayViewModel>()
            };

            // 2. הוספה לרשימה שמוצגת ב-TreeView
            GroupedProjects.Add(newFolder);

        }

        /// <summary>
        /// Triggers the event to open the selected project in the editor.
        /// </summary>
        private void OpenSelectedProject(object obj)
        {
            // בודק אם קיבלנו פרויקט מה-TreeView (דרך הפרמטר obj)
            // אם לא, לוקח את הפרויקט שנבחר ב-DataGrid (SelectedProject)
            var projectToOpen = (obj as ProjectDisplayViewModel) ?? SelectedProject;

            if (projectToOpen != null)
            {
                OnOpenProjectRequested?.Invoke(projectToOpen.Report, projectToOpen.CurrentUserRole);
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

