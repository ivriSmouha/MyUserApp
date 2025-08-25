using MyUserApp.Models;
using MyUserApp.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class ReportEntryViewModel : BaseViewModel
    {
        // Dropdown options
        public ObservableCollection<string> AircraftTypes { get; }
        public ObservableCollection<string> TailNumbers { get; }
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        public ObservableCollection<string> Usernames { get; }

        // User Selections
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

        public ObservableCollection<string> SelectedImagePaths { get; }

        // Commands
        public ICommand SelectImagesCommand { get; }
        public ICommand SubmitReportCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LogoutCommand { get; }

        // Events
        public event Action<InspectionReportModel> OnReportSubmitted;
        public event Action OnCancelled;
        public event Action OnLogoutRequested;

        public ReportEntryViewModel(UserModel user)
        {
            SelectImagesCommand = new RelayCommand(SelectImages);
            SubmitReportCommand = new RelayCommand(SubmitReport, _ => CanSubmitReport());
            CancelCommand = new RelayCommand(param => OnCancelled?.Invoke());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());

            SelectedImagePaths = new ObservableCollection<string>();
            var options = OptionsService.Instance.Options;
            AircraftTypes = new ObservableCollection<string>(options.AircraftTypes);
            TailNumbers = new ObservableCollection<string>(options.TailNumbers);
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);
            var allUsernames = UserService.Instance.Users.Select(u => u.Username);
            Usernames = new ObservableCollection<string>(allUsernames);

            SelectedInspector = user.Username;
        }

        private void SelectImages(object obj)
        {
            var openFileDialog = new OpenFileDialog { Multiselect = true, Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    SelectedImagePaths.Add(filename);
                }
            }
        }

        private bool CanSubmitReport()
        {
            return !string.IsNullOrEmpty(SelectedAircraftType) &&
                   !string.IsNullOrEmpty(SelectedTailNumber) &&
                   !string.IsNullOrEmpty(SelectedInspector) &&
                   SelectedImagePaths.Any();
        }

        private void SubmitReport(object obj)
        {
            string projectName = $"{SelectedAircraftType} - {SelectedTailNumber} ({SelectedAircraftSide})";
            string reportImageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportImages", Guid.NewGuid().ToString());
            Directory.CreateDirectory(reportImageFolder);

            var newImagePaths = new List<string>();
            foreach (string originalPath in SelectedImagePaths)
            {
                string fileName = Path.GetFileName(originalPath);
                string destinationPath = Path.Combine(reportImageFolder, fileName);
                try
                {
                    File.Copy(originalPath, destinationPath);
                    newImagePaths.Add(destinationPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying file {fileName}: {ex.Message}");
                }
            }

            var now = DateTime.Now;
            var newReport = new InspectionReportModel
            {
                ProjectName = projectName,
                AircraftType = SelectedAircraftType,
                TailNumber = SelectedTailNumber,
                AircraftSide = SelectedAircraftSide,
                Reason = SelectedReason,
                InspectorName = SelectedInspector,
                VerifierName = SelectedVerifier,
                ImagePaths = newImagePaths,
                CreationDate = now,
                LastModifiedDate = now
            };

            ReportService.Instance.AddReport(newReport);
            MessageBox.Show("Report submitted successfully!", "Success");
            OnReportSubmitted?.Invoke(newReport);
        }
    }
}