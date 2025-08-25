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

        /// <summary>
        /// Gets a value indicating whether the current user has both Inspector and Verifier roles on this project.
        /// </summary>
        public bool IsDualRole { get; }


        // --- Properties that delegate to the underlying Report model for easy data binding ---

        public string ProjectName => Report.ProjectName;
        public DateTime CreationDate => Report.CreationDate;
        public DateTime LastModifiedDate => Report.LastModifiedDate;
        public string InspectorName => Report.InspectorName;
        public string VerifierName => Report.VerifierName;

        /// <summary>
        /// A string representation of the user's role for display in the UI.
        /// </summary>
        public string RoleDisplayString
        {
            get
            {
                if (IsDualRole)
                {
                    return "Inspector / Verifier";
                }
                return CurrentUserRole.ToString();
            }
        }

        public ProjectDisplayViewModel(InspectionReportModel report, UserModel currentUser)
        {
            Report = report;

            bool isInspector = report.InspectorName == currentUser.Username;
            bool isVerifier = report.VerifierName == currentUser.Username;

            if (isInspector && isVerifier)
            {
                IsDualRole = true;
                CurrentUserRole = AuthorType.Inspector; // Default role for display
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