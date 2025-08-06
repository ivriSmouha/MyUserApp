using System;
using System.Collections.Generic;

namespace MyUserApp.Models
{
    /// <summary>
    /// Represents a single, complete inspection report. This is the central data
    /// object that is passed from the data entry screen to the new Image Editor screen.
    /// </summary>
    public class InspectionReportModel
    {
        /// <summary>
        /// A unique identifier for the report.
        /// </summary>
        public Guid ReportId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user-defined project name.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The date and time the report was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string AircraftType { get; set; }
        public string TailNumber { get; set; }
        public string AircraftSide { get; set; }
        public string Reason { get; set; }
        public string InspectorName { get; set; }
        public string VerifierName { get; set; }

        /// <summary>
        /// A list of file paths to the original, untouched images selected by the user.
        /// </summary>
        public List<string> ImagePaths { get; set; } = new List<string>();

        // This property can be activated later to save annotations to the report file.
        // public List<AnnotationModel> Annotations { get; set; } = new List<AnnotationModel>();
    }
}