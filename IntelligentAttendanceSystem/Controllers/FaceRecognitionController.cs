using IntelligentAttendanceSystem.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Controllers
{
    [AllowAnonymous]
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
        public async Task<JsonResult> StartRecognition([FromBody] int channel = 0)
        {
            try
            {
                bool isrunning = _faceRecognitionService.IsChannelRunning(channel);
                if (isrunning)
                {
                    return Json(new
                    {
                        success = isrunning,
                        message = isrunning ? "Face recognition started" : "Failed to start face recognition"
                    });
                }
                bool success = await _faceRecognitionService.StartFaceRecognitionAsync(channel);
                return Json(new { success, message = success ? $"Face recognition started on channel {channel}" : $"Failed to start face recognition on channel {channel}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting face recognition on channel {channel}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> StopRecognition([FromBody] int channel = 0)
        {
            try
            {
                bool success = await _faceRecognitionService.StopFaceRecognitionAsync(channel);
                return Json(new
                {
                    success,
                    message = success ? $"Face recognition stopped on channel {channel}" :
                    $"Failed to stop face recognition on channel {channel}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping face recognition on channel {channel}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetRunningChannels()
        {
            try
            {
                var channels = _faceRecognitionService.GetRunningChannels();
                return Json(new { success = true, channels });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting running channels");
                return Json(new { success = false, channels = new List<int>() });
            }
        }
        [HttpGet]
        public JsonResult GetChannelStatus(int channel)
        {
            try
            {
                var isRunning = _faceRecognitionService.IsChannelRunning(channel);
                // You might want to include additional stats like event counts
                return Json(new
                {
                    success = true,
                    channel = channel,
                    isRunning = isRunning,
                    //eventCount = 0 // Implement this based on your needs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting status for channel {channel}");
                return Json(new { success = false, channel = channel });
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
