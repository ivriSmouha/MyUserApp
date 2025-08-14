// File: MyUserApp/ViewModels/ProjectDisplayViewModel.cs
using MyUserApp.Models;
using System;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// A ViewModel that wraps an InspectionReportModel to provide additional properties
    /// for display purposes, such as the current user's role for that specific report.
    /// </summary>
    public class ProjectDisplayViewModel : BaseViewModel
    {
        // The underlying data model for the project.
        public InspectionReportModel Report { get; }

        /// <summary>
        /// The role of the currently logged-in user for this specific project.
        /// This is the key piece of context that the original model lacks.
        /// </summary>
        public AuthorType CurrentUserRole { get; }

        // --- Properties that delegate to the underlying Report model for easy data binding ---

        public string ProjectName => Report.ProjectName;
        public DateTime Timestamp => Report.Timestamp;
        public string InspectorName => Report.InspectorName;

        /// <summary>
        /// A string representation of the user's role for display in the UI.
        /// </summary>
        public string RoleDisplayString => CurrentUserRole.ToString();

        public ProjectDisplayViewModel(InspectionReportModel report, UserModel currentUser)
        {
            Report = report;

            // Determine the current user's role for this specific report.
            if (report.InspectorName == currentUser.Username)
            {
                CurrentUserRole = AuthorType.Inspector;
            }
            else if (report.VerifierName == currentUser.Username)
            {
                CurrentUserRole = AuthorType.Verifier;
            }
        }
    }
}