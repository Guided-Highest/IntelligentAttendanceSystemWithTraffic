using IntelligentAttendanceSystem.Interface;
using Microsoft.AspNetCore.Mvc;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Controllers
{
    public class FaceRecognitionController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IDahuaDeviceService _deviceService;
        private readonly ILogger<FaceRecognitionController> _logger;

        public FaceRecognitionController(
            IFaceRecognitionService faceRecognitionService,
            IDahuaDeviceService deviceService,
            ILogger<FaceRecognitionController> logger)
        {
            _faceRecognitionService = faceRecognitionService;
            _deviceService = deviceService;
            _logger = logger;

            // Subscribe to events
            _faceRecognitionService.OnFaceRecognitionEvent += OnFaceRecognitionEvent;
            _faceRecognitionService.OnFaceDetectionEvent += OnFaceDetectionEvent;
        }

        public IActionResult Index()
        {
            if (!_deviceService.IsDeviceConnected)
            {
                return RedirectToAction("DeviceDisconnected", "Device");
            }

            return View(new { chnl = _deviceService.DeviceInfo.nChanNum, IsFRS = _faceRecognitionService.IsFaceRecognationStart });
        }

        [HttpPost]
        public async Task<JsonResult> StartRecognition(int channel = 0)
        {
            try
            {
                if (_faceRecognitionService.IsFaceRecognationStart)
                {
                    return Json(new
                    {
                        success = _faceRecognitionService.IsFaceRecognationStart,
                        message = _faceRecognitionService.IsFaceRecognationStart ? "Face recognition started" : "Failed to start face recognition"
                    });
                }
                bool success = await _faceRecognitionService.StartFaceRecognitionAsync(channel);
                return Json(new { success, message = success ? "Face recognition started" : "Failed to start face recognition" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting face recognition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> StopRecognition()
        {
            try
            {
                bool success = await _faceRecognitionService.StopFaceRecognitionAsync();
                return Json(new { success, message = success ? "Face recognition stopped" : "Failed to stop face recognition" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping face recognition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetStatus()
        {
            try
            {
                var status = await _faceRecognitionService.GetStatusAsync();
                return Json(new
                {
                    status.IsRunning,
                    status.Channel,
                    AnalyzerID = status.AnalyzerID.ToString(),
                    status.LastEventTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting face recognition status");
                return Json(new { IsRunning = false, Error = ex.Message });
            }
        }

        private void OnFaceRecognitionEvent(FaceRecognitionEvent faceEvent)
        {
            // Here you can:
            // 1. Store the event in database
            // 2. Send real-time updates via SignalR
            // 3. Log the event
            // 4. Trigger other business logic

            _logger.LogInformation($"Face recognition event: {faceEvent.CandidateInfo?.Name ?? "Unknown"} with similarity {faceEvent.Similarity}");

            // Example: You could integrate with your attendance system here
            if (faceEvent.Similarity >= 80) // Threshold for successful recognition
            {
                // Log attendance
                LogAttendance(faceEvent);
            }
        }

        private void OnFaceDetectionEvent(FaceRecognitionEvent faceEvent)
        {
            _logger.LogInformation($"Face detection event: {faceEvent.FaceAttributes.Sex}, {faceEvent.FaceAttributes.Age} years");

            // Handle face detection events (unknown faces)
        }

        private void LogAttendance(FaceRecognitionEvent faceEvent)
        {
            // Implement your attendance logging logic here
            // This would typically save to your database
            _logger.LogInformation($"Attendance logged for: {faceEvent.CandidateInfo.Name}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _faceRecognitionService.OnFaceRecognitionEvent -= OnFaceRecognitionEvent;
                _faceRecognitionService.OnFaceDetectionEvent -= OnFaceDetectionEvent;
            }
            base.Dispose(disposing);
        }
    }
}
