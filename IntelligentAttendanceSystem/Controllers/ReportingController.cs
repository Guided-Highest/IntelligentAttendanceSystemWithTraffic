using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelligentAttendanceSystem.Controllers
{
    // Controllers/ReportingController.cs
    public class ReportingController : Controller
    {
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportingController> _logger;
        private readonly ApplicationDbContext _context;

        public ReportingController(
            IReportingService reportingService,
            ILogger<ReportingController> logger, ApplicationDbContext context)
        {
            _reportingService = reportingService;
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(ReportRequest request)
        {
            try
            {
                var report = await _reportingService.GetFaceRecognitionReportAsync(request);
                return View("ReportView", report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["ErrorMessage"] = "Error generating report";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadExcel(ReportRequest request)
        {
            try
            {
                var stream = await _reportingService.GenerateExcelReportAsync(request);
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           $"FaceRecognition_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel report");
                TempData["ErrorMessage"] = "Error generating Excel report";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdf(ReportRequest request)
        {
            try
            {
                var stream = await _reportingService.GeneratePdfReportAsync(request);
                return File(stream, "application/pdf",
                           $"FaceRecognition_Report_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF report");
                TempData["ErrorMessage"] = "Error generating PDF report";
                return RedirectToAction("Index");
            }
        }
        [HttpGet("GetRecognitionChartData")]
        public async Task<ActionResult<ChartData>> GetRecognitionChartData([FromQuery] string period = "week")
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var data = await GetRecognitionChartDataAsync(period, cts.Token);
                return Ok(data);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chart data request timed out");
                return StatusCode(408, "Request timeout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chart data");
                return StatusCode(500, "Error loading chart data");
            }
        }

        private async Task<ChartData> GetRecognitionChartDataAsync(string period, CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            DateTime startDate;
            string groupFormat;

            switch (period.ToLower())
            {
                case "day":
                    startDate = today;
                    groupFormat = "HH"; // Group by hour
                    break;
                case "month":
                    startDate = today.AddDays(-30);
                    groupFormat = "yyyy-MM-dd"; // Group by day
                    break;
                case "week":
                default:
                    startDate = today.AddDays(-7);
                    groupFormat = "yyyy-MM-dd"; // Group by day
                    break;
            }

            // Get records for the period
            var records = await _context.FaceAttendanceRecords
                .Where(r => r.EventTime >= startDate && r.EventTime < tomorrow && r.EventType == "RECOGNITION")
                .Select(r => new { r.EventTime, r.Similarity })
                .ToListAsync(cancellationToken);

            // Group and format data
            var groupedData = records
                .GroupBy(r =>
                {
                    var eventTime = r.EventTime;
                    return period.ToLower() switch
                    {
                        "day" => eventTime.ToString("HH:00"),
                        _ => eventTime.ToString("MMM dd")
                    };
                })
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count(),
                    AverageSimilarity = g.Average(r => r.Similarity)
                })
                .OrderBy(x => x.Label)
                .ToList();

            // Fill in missing data points for continuity
            var filledData = FillMissingDataPoints(groupedData, period, startDate, tomorrow);

            return new ChartData
            {
                Period = period,
                DataPoints = filledData,
                TotalRecognitions = records.Count,
                AverageSimilarity = records.Any() ? records.Average(r => r.Similarity) : 0
            };
        }

        [HttpGet("GetUserRecognitionStats")]
        public async Task<ActionResult<UserRecognitionChartData>> GetUserRecognitionStats([FromQuery] int topUsers = 10)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var data = await GetUserRecognitionStatsAsync(topUsers, cts.Token);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user recognition stats");
                return StatusCode(500, "Error loading user recognition statistics");
            }
        }

        // Using the simpler approach without complex Join
        private async Task<UserRecognitionChartData> GetUserRecognitionStatsAsync(int topUsers, CancellationToken cancellationToken = default)
        {
            var weekStart = DateTime.Today.AddDays(-7);

            // Get user recognition counts and average similarity
            var userRecognitionData = await _context.FaceAttendanceRecords
                .Where(r => r.EventTime >= weekStart &&
                           r.EventType == "RECOGNITION" &&
                           r.UserId != null)
                .GroupBy(r => r.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    RecognitionCount = g.Count(),
                    AverageSimilarity = g.Average(r => r.Similarity)
                })
                .OrderByDescending(x => x.RecognitionCount)
                .Take(topUsers)
                .ToListAsync(cancellationToken);

            if (!userRecognitionData.Any())
            {
                return new UserRecognitionChartData { UserStats = new List<UserRecognitionStat>(), Period = "week" };
            }

            // Get user names
            var userIds = userRecognitionData.Select(urc => urc.UserId).ToList();
            var users = await _context.FaceUsers
                .Where(u => userIds.Contains(u.Id.ToString()))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

            // Combine the data
            var userStats = userRecognitionData.Select(urc => new UserRecognitionStat
            {
                UserId = Convert.ToInt32(urc.UserId),
                UserName = users.ContainsKey(Convert.ToInt32(urc.UserId)) ? users[Convert.ToInt32(urc.UserId)] : "Unknown User",
                RecognitionCount = urc.RecognitionCount,
                AverageSimilarity = urc.AverageSimilarity
            }).ToList();

            return new UserRecognitionChartData
            {
                UserStats = userStats,
                Period = "week"
            };
        }

        private List<ChartDataPoint> FillMissingDataPoints(List<ChartDataPoint> data, string period, DateTime startDate, DateTime endDate)
        {
            var allLabels = new List<string>();
            var current = startDate;

            while (current < endDate)
            {
                string label = period.ToLower() switch
                {
                    "day" => current.ToString("HH:00"),
                    _ => current.ToString("MMM dd")
                };
                allLabels.Add(label);

                current = period.ToLower() switch
                {
                    "day" => current.AddHours(1),
                    _ => current.AddDays(1)
                };
            }

            var result = new List<ChartDataPoint>();
            foreach (var label in allLabels)
            {
                var existing = data.FirstOrDefault(d => d.Label == label);
                result.Add(existing ?? new ChartDataPoint
                {
                    Label = label,
                    Value = 0,
                    AverageSimilarity = 0
                });
            }

            return result;
        }
        [HttpGet]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats()
        {
            try
            {
                // Set a reasonable timeout (30 seconds for dashboard stats)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var stats = await _reportingService.GetDashboardStatsAsync(cts.Token);
                return Ok(stats);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Dashboard stats request timed out");
                return StatusCode(408, "Request timeout"); // Request timeout
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats");
                return StatusCode(500, "Error loading dashboard statistics");
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetAttendanceData(DateTime startDate, DateTime endDate)
        {
            try
            {
                var request = new ReportRequest { StartDate = startDate, EndDate = endDate };
                var records = await _reportingService.GetAttendanceRecordsAsync(request);
                return Json(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance data");
                return Json(new { error = ex.Message });
            }
        }
    }
}
