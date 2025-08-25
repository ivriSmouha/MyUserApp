using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyUserApp.Models
{
    /// <summary>
    /// Represents the complete data model for a single inspection report.
    /// This class aggregates all information related to an inspection.
    /// </summary>
    public class InspectionReportModel
    {
        /// <summary>
        /// A unique identifier for the report.
        /// </summary>
        public Guid ReportId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user-facing name of the project, often generated from other details.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The date and time when the report was first created.
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// The date and time when the report was last modified.
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// The type of aircraft being inspected.
        /// </summary>
        public string AircraftType { get; set; }

        /// <summary>
        /// The unique tail number or identifier of the aircraft.
        /// </summary>
        public string TailNumber { get; set; }

        /// <summary>
        /// The specific side or section of the aircraft under inspection.
        /// </summary>
        public string AircraftSide { get; set; }

        /// <summary>
        /// The reason or purpose for conducting the inspection.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// The username of the inspector assigned to this report.
        /// </summary>
        public string InspectorName { get; set; }

        /// <summary>
        /// The username of the verifier assigned to this report.
        /// </summary>
        public string VerifierName { get; set; }

        /// <summary>
        /// A list of full file paths to the images included in this report.
        /// </summary>
        public List<string> ImagePaths { get; set; } = new List<string>();

        /// <summary>
        /// A dictionary that maps an image file path to its list of annotations.
        /// This structure is used for saving and loading annotations.
        /// </summary>
        public Dictionary<string, List<AnnotationModel>> AnnotationsByImage { get; set; } = new Dictionary<string, List<AnnotationModel>>();

        /// <summary>
        /// A dictionary that maps an image file path to its brightness/contrast adjustments.
        /// </summary>
        public Dictionary<string, ImageAdjustmentModel> AdjustmentsByImage { get; set; } = new Dictionary<string, ImageAdjustmentModel>();
    }
}