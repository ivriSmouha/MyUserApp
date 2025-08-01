﻿using MyUserApp.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MyUserApp.Services
{
    // Singleton service to manage all inspection report data.
    public class ReportService
    {
        private const string FilePath = "inspection_reports.json";
        private List<InspectionReportModel> _allReports;
        private static readonly ReportService _instance = new ReportService();
        public static ReportService Instance => _instance;

        private ReportService() { LoadReports(); }

        private void LoadReports()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _allReports = JsonSerializer.Deserialize<List<InspectionReportModel>>(json);
            }
            else
            {
                _allReports = new List<InspectionReportModel>();
            }
        }

        private void SaveReports()
        {
            var json = JsonSerializer.Serialize(_allReports, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public void AddReport(InspectionReportModel report)
        {
            _allReports.Add(report);
            SaveReports();
        }

        public List<InspectionReportModel> GetReportsForUser(string username)
        {
            if (string.IsNullOrEmpty(username)) return new List<InspectionReportModel>();
            return _allReports
                .Where(r => r.InspectorName == username || r.VerifierName == username)
                .OrderByDescending(r => r.Timestamp)
                .ToList();
        }

        public List<InspectionReportModel> GetAllReports()
        {
            return _allReports.OrderByDescending(r => r.Timestamp).ToList();
        }
    }
}