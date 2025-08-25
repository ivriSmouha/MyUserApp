using MyUserApp.Models;
using System;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// A wrapper ViewModel for an InspectionReportModel. It provides additional
    /// display-specific properties, like the current user's role for that report,
    /// without modifying the underlying data model.
    /// </summary>
    public class ProjectDisplayViewModel : BaseViewModel
    {
        /// <summary>
        /// The core data model for the inspection report.
        /// </summary>
        public InspectionReportModel Report { get; }

        /// <summary>
        /// The role (Inspector or Verifier) of the currently logged-in user
        /// for this specific project.
        /// </summary>
        public AuthorType CurrentUserRole { get; }

        /// <summary>
        /// Gets a value indicating whether the current user is assigned as
        /// both the Inspector and the Verifier for this project.
        /// </summary>
        public bool IsDualRole { get; }

        // Delegating properties that expose data from the Report model for easy binding.
        public string ProjectName => Report.ProjectName;
        public DateTime CreationDate => Report.CreationDate;
        public DateTime LastModifiedDate => Report.LastModifiedDate;
        public string InspectorName => Report.InspectorName;
        public string VerifierName => Report.VerifierName;

        /// <summary>
        /// A formatted string representing the user's role(s) for display in the UI.
        /// </summary>
        public string RoleDisplayString
        {
            get
            {
                return IsDualRole ? "Inspector / Verifier" : CurrentUserRole.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of the ProjectDisplayViewModel.
        /// </summary>
        /// <param name="report">The inspection report to wrap.</param>
        /// <param name="currentUser">The currently logged-in user.</param>
        public ProjectDisplayViewModel(InspectionReportModel report, UserModel currentUser)
        {
            Report = report;

            // Determine the user's role(s) for this specific report.
            bool isInspector = report.InspectorName == currentUser.Username;
            bool isVerifier = report.VerifierName == currentUser.Username;

            if (isInspector && isVerifier)
            {
                IsDualRole = true;
                CurrentUserRole = AuthorType.Inspector; // Default to Inspector for opening the project.
            }
            else if (isInspector)
            {
                IsDualRole = false;
                CurrentUserRole = AuthorType.Inspector;
            }
            else if (isVerifier)
            {
                IsDualRole = false;
                CurrentUserRole = AuthorType.Verifier;
            }
        }
    }
}