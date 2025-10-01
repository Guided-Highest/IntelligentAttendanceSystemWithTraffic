using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Services
{
    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(
        ApplicationDbContext context,
        ILogger<ReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FaceRecognitionReport> GetFaceRecognitionReportAsync(ReportRequest request)
        {
            try
            {
                // Get attendance records without invalid Include
                var records = await _context.FaceAttendanceRecords
                    .Where(r => r.EventTime >= request.StartDate && r.EventTime <= request.EndDate)
                    .ToListAsync();

                // If you need user details, you can join with FaceUsers table
                var users = await _context.FaceUsers.ToDictionaryAsync(u => u.UserId, u => u);

                var report = new FaceRecognitionReport
                {
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TotalRecognitions = records.Count(r => r.EventType == "RECOGNITION"),
                    TotalDetections = records.Count(r => r.EventType == "DETECTION"),
                    UniqueUsers = records.Select(r => r.UserId).Distinct().Count(),
                    AverageSimilarity = records.Any() ? records.Average(r => r.Similarity) : 0,
                    HourlyStats = GenerateHourlyStats(records, request.StartDate, request.EndDate),
                    TopUsers = GenerateTopUsers(records, users),
                    DepartmentStats = GenerateDepartmentStats(records, users)
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating face recognition report");
                throw;
            }
        }
        private List<RecognitionStats> GenerateHourlyStats(List<FaceAttendanceRecord> records, DateTime start, DateTime end)
        {
            var hourlyStats = new List<RecognitionStats>();

            for (int hour = 0; hour < 24; hour++)
            {
                var hourRecords = records.Where(r => r.EventTime.Hour == hour).ToList();
                hourlyStats.Add(new RecognitionStats
                {
                    Hour = hour,
                    Recognitions = hourRecords.Count(r => r.EventType == "RECOGNITION"),
                    Detections = hourRecords.Count(r => r.EventType == "DETECTION")
                });
            }

            return hourlyStats;
        }

        private List<UserRecognitionCount> GenerateTopUsers(List<FaceAttendanceRecord> records, Dictionary<string, FaceUser> users, int topCount = 10)
        {
            var userStats = records
                .Where(r => r.EventType == "RECOGNITION" && !string.IsNullOrEmpty(r.UserId))
                .GroupBy(r => r.UserId)
                .Select(g => new UserRecognitionCount
                {
                    UserId = g.Key,
                    UserName = users.ContainsKey(g.Key) ? users[g.Key].Name : g.First().UserName ?? "Unknown",
                    RecognitionCount = g.Count(),
                    AverageSimilarity = g.Average(r => r.Similarity)
                })
                .OrderByDescending(u => u.RecognitionCount)
                .Take(topCount)
                .ToList();

            return userStats;
        }

        private List<DepartmentStats> GenerateDepartmentStats(List<FaceAttendanceRecord> records, Dictionary<string, FaceUser> users)
        {
            var departmentStats = records
                .Where(r => r.EventType == "RECOGNITION" && !string.IsNullOrEmpty(r.UserId))
                .GroupBy(r =>
                {
                    if (users.ContainsKey(r.UserId) && !string.IsNullOrEmpty(users[r.UserId].Department))
                        return users[r.UserId].Department;
                    return "Unknown Department";
                })
                .Select(g => new DepartmentStats
                {
                    Department = g.Key,
                    RecognitionCount = g.Count(),
                    UserCount = g.Select(r => r.UserId).Distinct().Count()
                })
                .OrderByDescending(d => d.RecognitionCount)
                .ToList();

            return departmentStats;
        }

        public async Task<MemoryStream> GenerateExcelReportAsync(ReportRequest request)
        {
            var report = await GetFaceRecognitionReportAsync(request);

            // Create Excel package (you'll need to install EPPlus package)
            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Face Recognition Report");

            // Add headers
            worksheet.Cells[1, 1].Value = "Face Recognition Report";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;

            worksheet.Cells[2, 1].Value = "Period:";
            worksheet.Cells[2, 2].Value = $"{report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}";

            worksheet.Cells[3, 1].Value = "Total Recognitions:";
            worksheet.Cells[3, 2].Value = report.TotalRecognitions;

            worksheet.Cells[4, 1].Value = "Total Detections:";
            worksheet.Cells[4, 2].Value = report.TotalDetections;

            worksheet.Cells[5, 1].Value = "Unique Users:";
            worksheet.Cells[5, 2].Value = report.UniqueUsers;

            worksheet.Cells[6, 1].Value = "Average Similarity:";
            worksheet.Cells[6, 2].Value = report.AverageSimilarity.ToString("F2") + "%";

            // Add hourly stats
            worksheet.Cells[8, 1].Value = "Hourly Statistics";
            worksheet.Cells[8, 1].Style.Font.Bold = true;

            worksheet.Cells[9, 1].Value = "Hour";
            worksheet.Cells[9, 2].Value = "Recognitions";
            worksheet.Cells[9, 3].Value = "Detections";

            for (int i = 0; i < report.HourlyStats.Count; i++)
            {
                worksheet.Cells[10 + i, 1].Value = report.HourlyStats[i].Hour + ":00";
                worksheet.Cells[10 + i, 2].Value = report.HourlyStats[i].Recognitions;
                worksheet.Cells[10 + i, 3].Value = report.HourlyStats[i].Detections;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }

        public async Task<MemoryStream> GeneratePdfReportAsync(ReportRequest request)
        {
            var report = await GetFaceRecognitionReportAsync(request);
            var stream = new MemoryStream();

            // For PDF generation, you would typically use a library like iTextSharp
            // This is a simplified example - you'd need to implement proper PDF generation

            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("Face Recognition Report");
                writer.WriteLine("=======================");
                writer.WriteLine($"Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
                writer.WriteLine($"Total Recognitions: {report.TotalRecognitions}");
                writer.WriteLine($"Total Detections: {report.TotalDetections}");
                writer.WriteLine($"Unique Users: {report.UniqueUsers}");
                writer.WriteLine($"Average Similarity: {report.AverageSimilarity:F2}%");
                writer.WriteLine();
                writer.WriteLine("Hourly Statistics:");
                writer.WriteLine("Hour\tRecognitions\tDetections");

                foreach (var stat in report.HourlyStats)
                {
                    writer.WriteLine($"{stat.Hour}:00\t{stat.Recognitions}\t{stat.Detections}");
                }
            }

            stream.Position = 0;
            return stream;
        }

        public async Task<List<FaceAttendanceRecord>> GetAttendanceRecordsAsync(ReportRequest request)
        {
            return await _context.FaceAttendanceRecords
                .Where(r => r.EventTime >= request.StartDate && r.EventTime <= request.EndDate)
                .OrderByDescending(r => r.EventTime)
                .ToListAsync();
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;
            var recordsToday = await _context.FaceAttendanceRecords
                .Where(r => r.EventTime >= today && r.EventTime < today.AddDays(1))
                .ToListAsync();

            var recordsThisWeek = await _context.FaceAttendanceRecords
                .Where(r => r.EventTime >= today.AddDays(-7))
                .ToListAsync();

            return new DashboardStats
            {
                TodayRecognitions = recordsToday.Count(r => r.EventType == "RECOGNITION"),
                WeekRecognitions = recordsThisWeek.Count(r => r.EventType == "RECOGNITION"),
                TotalUsers = await _context.FaceUsers.CountAsync(u => u.IsActive),
                AverageSimilarityToday = recordsToday.Any() ? recordsToday.Average(r => r.Similarity) : 0
            };
        }

        public async Task<List<AttendanceWithUserDetails>> GetDetailedAttendanceAsync(ReportRequest request)
        {
            var records = await _context.FaceAttendanceRecords
                .Where(r => r.EventTime >= request.StartDate && r.EventTime <= request.EndDate)
                .OrderByDescending(r => r.EventTime)
                .ToListAsync();

            var users = await _context.FaceUsers.ToDictionaryAsync(u => u.UserId, u => u);

            var detailedRecords = records.Select(r => new AttendanceWithUserDetails
            {
                AttendanceRecord = r,
                User = users.ContainsKey(r.UserId) ? users[r.UserId] : null
            }).ToList();

            return detailedRecords;
        }

    }
}
