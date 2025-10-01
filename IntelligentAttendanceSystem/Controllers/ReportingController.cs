using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    // Controllers/ReportingController.cs
    public class ReportingController : Controller
    {
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(
            IReportingService reportingService,
            ILogger<ReportingController> logger)
        {
            _reportingService = reportingService;
            _logger = logger;
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

        [HttpGet]
        public async Task<JsonResult> GetDashboardStats()
        {
            try
            {
                var stats = await _reportingService.GetDashboardStatsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { error = ex.Message });
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
