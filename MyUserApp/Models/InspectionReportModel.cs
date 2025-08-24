// File: MyUserApp/Models/InspectionReportModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Required for ObservableCollection

namespace MyUserApp.Models
{
    /// <summary>
    /// Represents a single, complete inspection report.
    /// </summary>
    public class InspectionReportModel
    {
        public Guid ReportId { get; set; } = Guid.NewGuid();
        public string ProjectName { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AircraftType { get; set; }
        public string TailNumber { get; set; }
        public string AircraftSide { get; set; }
        public string Reason { get; set; }
        public string InspectorName { get; set; }
        public string VerifierName { get; set; }
        public List<string> ImagePaths { get; set; } = new List<string>();
        /// <summary>
        /// Stores all annotations for the report, keyed by the image file path.
        /// This allows the application to save and load the user's work.
        /// We use List<AnnotationModel> for serialization, as ObservableCollection is for UI.
        /// </summary>
        public Dictionary<string, List<AnnotationModel>> AnnotationsByImage { get; set; } = new Dictionary<string, List<AnnotationModel>>();
        public Dictionary<string, ImageAdjustmentModel> AdjustmentsByImage { get; set; } = new Dictionary<string, ImageAdjustmentModel>();

    }
}