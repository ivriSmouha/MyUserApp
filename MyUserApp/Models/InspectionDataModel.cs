using System;
using System.Collections.Generic;
namespace MyUserApp.Models
{
    // This class represents a single data entry report.
    public class InspectionReportModel
    {
        public string ProjectName { get; set; }
        public Guid ReportId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AircraftType { get; set; }
        public string TailNumber { get; set; }
        public string AircraftSide { get; set; }
        public string Reason { get; set; }
        public string InspectorName { get; set; }
        public string VerifierName { get; set; }
        public List<string> ImagePaths { get; set; } = new List<string>();
    }
}