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
        public ObservableCollection<string> TailNumbers { get; } // NEW
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        public ObservableCollection<string> Usernames { get; }

        // --- Form Data Properties ---
        private string _selectedAircraftType;
        public string SelectedAircraftType { get => _selectedAircraftType; set { _selectedAircraftType = value; OnPropertyChanged(); } }
        private string _selectedTailNumber; // RENAMED
        public string SelectedTailNumber { get => _selectedTailNumber; set { _selectedTailNumber = value; OnPropertyChanged(); } }
        private string _selectedAircraftSide;
        public string SelectedAircraftSide { get => _selectedAircraftSide; set { _selectedAircraftSide = value; OnPropertyChanged(); } }
        private string _selectedReason;
        public string SelectedReason { get => _selectedReason; set { _selectedReason = value; OnPropertyChanged(); } }
        private string _selectedInspector;
        public string SelectedInspector { get => _selectedInspector; set { _selectedInspector = value; OnPropertyChanged(); } }
        private string _selectedVerifier;
        public string SelectedVerifier { get => _selectedVerifier; set { _selectedVerifier = value; OnPropertyChanged(); } }

        // --- Other properties and commands (no changes) ---
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
            TailNumbers = new ObservableCollection<string>(options.TailNumbers); // NEW
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);

            var allUsernames = UserService.Instance.Users.Select(u => u.Username);
            Usernames = new ObservableCollection<string>(allUsernames);

            // Set the inspector to the current user by default, but it will be selectable.
            SelectedInspector = user.Username;

            // Initialize commands
            SelectedImagePaths = new ObservableCollection<string>();
            SelectImagesCommand = new RelayCommand(SelectImages);
            SubmitReportCommand = new RelayCommand(SubmitReport, _ => CanSubmitReport());
            CancelCommand = new RelayCommand(param => OnFinished?.Invoke());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
        }

        private void SubmitReport(object obj)
        {
            string projectName = $"{this.SelectedAircraftType} - {this.SelectedTailNumber} ({this.SelectedAircraftSide})";

            // ... (image saving logic remains the same)

            var newReport = new InspectionReportModel
            {
                ProjectName = projectName,
                AircraftType = this.SelectedAircraftType,
                TailNumber = this.SelectedTailNumber, // RENAMED
                AircraftSide = this.SelectedAircraftSide,
                Reason = this.SelectedReason,
                InspectorName = this.SelectedInspector,
                VerifierName = this.SelectedVerifier,
                // ImagePaths = newImagePaths
            };
            ReportService.Instance.AddReport(newReport);
            MessageBox.Show("Report submitted successfully!", "Success");
            OnFinished?.Invoke();
        }

        private bool CanSubmitReport() => !string.IsNullOrEmpty(SelectedAircraftType) && !string.IsNullOrEmpty(SelectedTailNumber) && !string.IsNullOrEmpty(SelectedInspector);
        private void SelectImages(object obj)
        { /* ... */ }
    }
}