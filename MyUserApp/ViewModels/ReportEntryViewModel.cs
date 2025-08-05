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
        public ObservableCollection<string> AircraftTypes { get; }
        public ObservableCollection<string> TailNumbers { get; }
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        public ObservableCollection<string> Usernames { get; }
        public ObservableCollection<string> TailNumbers { get; }
        private string _selectedAircraftType;
        public string SelectedAircraftType { get => _selectedAircraftType; set { _selectedAircraftType = value; OnPropertyChanged(); } }

        private string _tailNumber;
        public string SelectedTailNumber { get => _tailNumber; set { _tailNumber = value; OnPropertyChanged(); } }

        private string _selectedAircraftSide;
        public string SelectedAircraftSide { get => _selectedAircraftSide; set { _selectedAircraftSide = value; OnPropertyChanged(); } }

        private string _selectedReason;
        public string SelectedReason { get => _selectedReason; set { _selectedReason = value; OnPropertyChanged(); } }
        private string _selectedInspector;
        public string SelectedInspector { get => _selectedInspector; set { _selectedInspector = value; OnPropertyChanged(); } }
        private string _selectedVerifier;
        public string SelectedVerifier { get => _selectedVerifier; set { _selectedVerifier = value; OnPropertyChanged(); } }
        public ObservableCollection<string> SelectedImagePaths { get; }
        public ICommand SelectImagesCommand { get; }
        public ICommand SubmitReportCommand { get; }
        public ICommand CancelCommand { get; }
        public event Action OnFinished;
        public ICommand LogoutCommand { get; }
        public event Action OnLogoutRequested;

        public ReportEntryViewModel(UserModel user)
        {
            var options = OptionsService.Instance.Options;
            AircraftTypes = new ObservableCollection<string>(options.AircraftTypes);
            TailNumbers = new ObservableCollection<string>(options.TailNumbers);
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);

            // --- NEW: Load usernames from the UserService ---
            // Use LINQ's .Select() to get just the Username string from each UserModel
            var allUsernames = UserService.Instance.Users.Select(u => u.Username);
            Usernames = new ObservableCollection<string>(allUsernames);
            SelectedInspector = user.Username;

            // ... (Initialize commands and image paths as before)
            SelectedImagePaths = new ObservableCollection<string>();
            SelectImagesCommand = new RelayCommand(SelectImages);
            SubmitReportCommand = new RelayCommand(SubmitReport, _ => CanSubmitReport());
            CancelCommand = new RelayCommand(param => OnFinished?.Invoke());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
        }

        private void SubmitReport(object obj)
        {
            string projectName = $"{this.SelectedAircraftType} - {this.SelectedTailNumber} ({this.SelectedAircraftSide})";
            string reportImageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportImages", Guid.NewGuid().ToString());
            Directory.CreateDirectory(reportImageFolder);
            var newImagePaths = new System.Collections.Generic.List<string>();
            foreach (string originalPath in this.SelectedImagePaths)
            {
                string fileName = Path.GetFileName(originalPath);
                string destinationPath = Path.Combine(reportImageFolder, fileName);
                try { File.Copy(originalPath, destinationPath); newImagePaths.Add(destinationPath); }
                catch (Exception ex) { MessageBox.Show($"Error copying file {fileName}: {ex.Message}"); }
            }
            var newReport = new InspectionReportModel
            {
                ProjectName = projectName,
                AircraftType = this.SelectedAircraftType,
                TailNumber = this.SelectedTailNumber,
                AircraftSide = this.SelectedAircraftSide,
                Reason = this.SelectedReason,
                InspectorName = this.SelectedInspector,
                VerifierName = this.SelectedVerifier,
                ImagePaths = newImagePaths
            };
            ReportService.Instance.AddReport(newReport);
            MessageBox.Show("Report submitted successfully!", "Success");
            OnFinished?.Invoke();
        }

        private bool CanSubmitReport() => !string.IsNullOrEmpty(SelectedAircraftType) && !string.IsNullOrEmpty(TailNumber) && !string.IsNullOrEmpty(SelectedInspector);
        private void SelectImages(object obj)
        {
            var openFileDialog = new OpenFileDialog { Multiselect = true, Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames) { SelectedImagePaths.Add(filename); }
            }
        }
    }
}