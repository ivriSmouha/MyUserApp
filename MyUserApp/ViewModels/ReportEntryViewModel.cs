// ViewModels/ReportEntryViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class ReportEntryViewModel : BaseViewModel
    {
        // --- All existing properties remain the same ---
        public ObservableCollection<string> AircraftTypes { get; }
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        private string _selectedAircraftType;
        public string SelectedAircraftType { get => _selectedAircraftType; set { _selectedAircraftType = value; OnPropertyChanged(); } }
        private string _tailNumber;
        public string TailNumber { get => _tailNumber; set { _tailNumber = value; OnPropertyChanged(); } }
        private string _selectedAircraftSide;
        public string SelectedAircraftSide { get => _selectedAircraftSide; set { _selectedAircraftSide = value; OnPropertyChanged(); } }
        private string _selectedReason;
        public string SelectedReason { get => _selectedReason; set { _selectedReason = value; OnPropertyChanged(); } }
        private string _inspectorName;
        public string InspectorName { get => _inspectorName; set { _inspectorName = value; OnPropertyChanged(); } }
        private string _verifierName;
        public string VerifierName { get => _verifierName; set { _verifierName = value; OnPropertyChanged(); } }
        public ObservableCollection<string> SelectedImagePaths { get; }
        public ICommand SelectImagesCommand { get; }
        public ICommand SubmitReportCommand { get; }
        public ICommand CancelCommand { get; }
        public event Action OnFinished;

        // --- CHANGE #1: Add the LogoutCommand property and its event ---
        public ICommand LogoutCommand { get; }
        public event Action OnLogoutRequested;

        public ReportEntryViewModel(UserModel user)
        {
            InspectorName = user.Username;
            var options = OptionsService.Instance.Options;
            AircraftTypes = new ObservableCollection<string>(options.AircraftTypes);
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);
            SelectedImagePaths = new ObservableCollection<string>();

            // --- Initialize existing commands ---
            SelectImagesCommand = new RelayCommand(SelectImages);
            SubmitReportCommand = new RelayCommand(SubmitReport, CanSubmitReport);
            CancelCommand = new RelayCommand(param => OnFinished?.Invoke());

            // --- CHANGE #2: Initialize the new LogoutCommand ---
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
        }

        private void SubmitReport(object obj)
        {
            // --- NEW: Construct the project name from form data ---
            string projectName = $"{this.SelectedAircraftType} - {this.TailNumber} ({this.SelectedAircraftSide})";

            // Create the report model...
            var newReport = new InspectionReportModel
            {
                // --- NEW: Assign the constructed name ---
                ProjectName = projectName,

                // ... and assign the rest of the properties as before
                AircraftType = this.SelectedAircraftType,
                TailNumber = this.TailNumber,
                AircraftSide = this.SelectedAircraftSide,
                Reason = this.SelectedReason,
                InspectorName = this.InspectorName,
                VerifierName = this.VerifierName,
                ImagePaths = new System.Collections.Generic.List<string>(this.SelectedImagePaths) // Copy images logic should also be here if you have it
            };

            // Use the central service to add the report.
            ReportService.Instance.AddReport(newReport);

            MessageBox.Show("Report submitted successfully!", "Success");

            // Signal that we are finished.
            OnFinished?.Invoke();
        }

        private void SelectImages(object obj)
        {
            var openFileDialog = new OpenFileDialog { Multiselect = true, Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames) { SelectedImagePaths.Add(filename); }
            }
        }

        private bool CanSubmitReport(object obj) => !string.IsNullOrEmpty(SelectedAircraftType) && !string.IsNullOrEmpty(TailNumber) && !string.IsNullOrEmpty(InspectorName);
    }
}