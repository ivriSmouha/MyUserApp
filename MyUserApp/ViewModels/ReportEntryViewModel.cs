using MyUserApp.Models;
using MyUserApp.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the new report creation screen. It collects all the necessary
    /// information and images to create and submit a new InspectionReportModel.
    /// </summary>
    public class ReportEntryViewModel : BaseViewModel
    {
        #region Dropdown Data Sources
        public ObservableCollection<string> AircraftTypes { get; }
        public ObservableCollection<string> TailNumbers { get; }
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        public ObservableCollection<string> Usernames { get; }
        #endregion

        #region User Selections
        private string _selectedAircraftType;
        public string SelectedAircraftType { get => _selectedAircraftType; set { _selectedAircraftType = value; OnPropertyChanged(); ((RelayCommand)SubmitReportCommand).RaiseCanExecuteChanged(); } }

        private string _selectedTailNumber;
        public string SelectedTailNumber { get => _selectedTailNumber; set { _selectedTailNumber = value; OnPropertyChanged(); ((RelayCommand)SubmitReportCommand).RaiseCanExecuteChanged(); } }

        private string _selectedAircraftSide;
        public string SelectedAircraftSide { get => _selectedAircraftSide; set { _selectedAircraftSide = value; OnPropertyChanged(); } }

        private string _selectedReason;
        public string SelectedReason { get => _selectedReason; set { _selectedReason = value; OnPropertyChanged(); } }

        private string _selectedInspector;
        public string SelectedInspector { get => _selectedInspector; set { _selectedInspector = value; OnPropertyChanged(); ((RelayCommand)SubmitReportCommand).RaiseCanExecuteChanged(); } }

        private string _selectedVerifier;
        public string SelectedVerifier { get => _selectedVerifier; set { _selectedVerifier = value; OnPropertyChanged(); } }

        /// <summary>
        /// A collection of file paths for the images selected by the user.
        /// </summary>
        public ObservableCollection<string> SelectedImagePaths { get; }
        #endregion

        #region Commands
        public ICommand SelectImagesCommand { get; }
        public ICommand SubmitReportCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LogoutCommand { get; }
        #endregion

        #region Events
        /// <summary>
        /// Fired when a report is successfully created and submitted.
        /// </summary>
        public event Action<InspectionReportModel> OnReportSubmitted;

        /// <summary>
        /// Fired when the user cancels the report creation process.
        /// </summary>
        public event Action OnCancelled;

        /// <summary>
        /// Fired to request a logout from the main application view.
        /// </summary>
        public event Action OnLogoutRequested;
        #endregion

        /// <summary>
        /// Initializes the Report Entry ViewModel.
        /// </summary>
        /// <param name="user">The user who is creating the report.</param>
        public ReportEntryViewModel(UserModel user)
        {
            // Initialize commands.
            SelectImagesCommand = new RelayCommand(SelectImages);
            SubmitReportCommand = new RelayCommand(SubmitReport, _ => CanSubmitReport());
            CancelCommand = new RelayCommand(param => OnCancelled?.Invoke());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            SelectedImagePaths = new ObservableCollection<string>();

            // Populate dropdowns from global services.
            var options = OptionsService.Instance.Options;
            AircraftTypes = new ObservableCollection<string>(options.AircraftTypes);
            TailNumbers = new ObservableCollection<string>(options.TailNumbers);
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);
            Usernames = new ObservableCollection<string>(UserService.Instance.Users.Select(u => u.Username));

            // Default the inspector to the current user.
            SelectedInspector = user.Username;
        }

        /// <summary>
        /// Opens a file dialog to allow the user to select images for the report.
        /// </summary>
        private void SelectImages(object obj)
        {
            var openFileDialog = new OpenFileDialog { Multiselect = true, Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp" };

            if (openFileDialog.ShowDialog() == true)
            {
                // רשימה זמנית שתכיל את כל הנתיבים (החדשים והקיימים)
                var allPaths = SelectedImagePaths.ToList();
                allPaths.AddRange(openFileDialog.FileNames);

                // מיון הרשימה לפי הלוגיקה המבוקשת
                var sortedPaths = allPaths.OrderBy(path =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(path).ToUpper();

                    // חילוץ החלק של המספר/אות (למשל מ-"R11A" נוציא "11A")
                    // הנחה: השם מתחיל ב-R או L ואחריו הקוד
                    string code = fileName.Length > 1 ? fileName.Substring(1) : fileName;

                    // מציאת האינדקס ברשימה המוגדרת מראש
                    int index = CustomOrder.IndexOf(code);

                    // אם הקוד לא נמצא ברשימה, הוא יופיע בסוף (אינדקס גבוה)
                    return index == -1 ? int.MaxValue : index;
                })
                .ThenBy(path => path) // מיון משני לפי השם המלא למקרה של כפילויות
                .ToList();

             
                SelectedImagePaths.Clear();
                foreach (var path in sortedPaths)
                {
                    SelectedImagePaths.Add(path);
                }
            }
        }

        /// <summary>
        /// Determines if the report has enough information to be submitted.
        /// </summary>
        private bool CanSubmitReport()
        {
            return !string.IsNullOrEmpty(SelectedAircraftType) &&
                   !string.IsNullOrEmpty(SelectedTailNumber) &&
                   !string.IsNullOrEmpty(SelectedInspector) &&
                   SelectedImagePaths.Any();
        }
        //the list for the start prosject
        //and the numbers in eich ather
        private static readonly List<string> CustomOrder = new List<string>
        {
            "7", "8", "9", "10", "11", "11A", "12", "12A", "13", "14", "15", "16",
            "17H", "18H", "17", "18", "19", "20", "21H", "22H", "21", "22"
        };

        /// <summary>
        /// Gathers all the selected data, copies images to a dedicated report folder,
        /// creates a new report model, and submits it.
        /// </summary>
        private void SubmitReport(object obj)
        {
            // Create a dedicated folder to store the report's images.
            string reportImageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportImages", Guid.NewGuid().ToString());
            Directory.CreateDirectory(reportImageFolder);

            // Copy selected images to the new folder
            var newImagePaths = new List<string>();
            foreach (string originalPath in SelectedImagePaths)
            {
                string destinationPath = Path.Combine(reportImageFolder, Path.GetFileName(originalPath));
                try
                {
                    File.Copy(originalPath, destinationPath);
                    newImagePaths.Add(destinationPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying file {Path.GetFileName(originalPath)}: {ex.Message}");
                }
            }

            // Assemble the new report model.
            var newReport = new InspectionReportModel
            {
                ProjectName = $"{SelectedAircraftType} - {SelectedTailNumber} ({SelectedAircraftSide})",
                AircraftType = SelectedAircraftType,
                TailNumber = SelectedTailNumber,
                AircraftSide = SelectedAircraftSide,
                Reason = SelectedReason,
                InspectorName = SelectedInspector,
                VerifierName = SelectedVerifier,
                ImagePaths = newImagePaths,
                CreationDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            // Add the report to the central service and notify for navigation.
            ReportService.Instance.AddReport(newReport);
            MessageBox.Show("Report submitted successfully!", "Success");
            OnReportSubmitted?.Invoke(newReport);
        }
    }
}