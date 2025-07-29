// ViewModels/ReportEntryViewModel.cs
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
        // --- Dropdown options ---
        public ObservableCollection<string> AircraftTypes { get; }
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        public ObservableCollection<string> Usernames { get; }
        // --- NEW: A collection for the tail number dropdown ---
        public ObservableCollection<string> TailNumbers { get; }

        // --- Form Data Properties ---
        private string _selectedAircraftType;
        public string SelectedAircraftType { get => _selectedAircraftType; set { _selectedAircraftType = value; OnPropertyChanged(); } }

        // --- RENAMED: Changed from TailNumber to SelectedTailNumber ---
        private string _selectedTailNumber;
        public string SelectedTailNumber { get => _selectedTailNumber; set { _selectedTailNumber = value; OnPropertyChanged(); } }

        private string _selectedAircraftSide;
        public string SelectedAircraftSide { get => _selectedAircraftSide; set { _selectedAircraftSide = value; OnPropertyChanged(); } }
        private string _selectedReason;
        public string SelectedReason { get => _selectedReason; set { _selectedReason = value; OnPropertyChanged(); } }
        private string _selectedInspector;
        public string SelectedInspector { get => _selectedInspector; set { _selectedInspector = value; OnPropertyChanged(); } }
        private string _selectedVerifier;
        public string SelectedVerifier { get => _selectedVerifier; set { _selectedVerifier = value; OnPropertyChanged(); } }

        // ... (Image selection, commands, and events are the same)
        // ...

        public ReportEntryViewModel(UserModel user)
        {
            var options = OptionsService.Instance.Options;
            AircraftTypes = new ObservableCollection<string>(options.AircraftTypes);
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);
            // --- NEW: Load tail numbers from the service ---
            TailNumbers = new ObservableCollection<string>(options.TailNumbers);

            var allUsernames = UserService.Instance.Users.Select(u => u.Username);
            Usernames = new ObservableCollection<string>(allUsernames);
            SelectedInspector = user.Username;

            // ... (Initialize commands and image paths as before)
        }

        private void SubmitReport(object obj)
        {
            // --- MODIFIED: Use the selected values to build the project name ---
            string projectName = $"{this.SelectedAircraftType} - {this.SelectedTailNumber} ({this.SelectedAircraftSide})";

            // ... (Image copying logic is the same) ...

            var newReport = new InspectionReportModel
            {
                ProjectName = projectName,
                AircraftType = this.SelectedAircraftType,
                // --- MODIFIED: Use the selected value ---
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

        // --- MODIFIED: Update validation to check the selected tail number ---
        private bool CanSubmitReport() => !string.IsNullOrEmpty(SelectedAircraftType) && !string.IsNullOrEmpty(SelectedTailNumber) && !string.IsNullOrEmpty(SelectedInspector);

        // ... (SelectImages method is the same) ...
    }
}