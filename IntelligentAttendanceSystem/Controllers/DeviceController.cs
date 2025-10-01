using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    public class DeviceController : Controller
    {
        private readonly IDahuaDeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IDahuaDeviceService deviceService, ILogger<DeviceController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;

            // Subscribe to device status events
            _deviceService.DeviceDisconnected += OnDeviceDisconnected;
            _deviceService.DeviceConnectionStatusChanged += OnDeviceConnectionStatusChanged;
        }

        public async Task<IActionResult> Initialize()
        {
            // If device is already connected and initialized, redirect to attendance
            if (_deviceService.IsDeviceConnected && _deviceService.IsInitialized)
            {
                return RedirectToAction("Index", "FaceRecognition");
            }
            try
            {
                // Check if device credentials exist
                var credentials = await _deviceService.GetDeviceCredentialsAsync();
                if (credentials == null)
                {
                    _logger.LogInformation("No device credentials found, redirecting to setup");
                    return RedirectToAction("NoDeviceFound");
                }

                // Attempt to login (manual retry)
                bool loginSuccess = await _deviceService.InitializeAndLoginAsync();

                if (loginSuccess)
                {
                    return RedirectToAction("Index", "FaceRecognition");
                }
                else
                {
                    // Store the error message for the LoginFailed page
                    TempData["ErrorMessage"] = _deviceService.InitializationError ?? "Login failed for unknown reason";
                    return RedirectToAction("LoginFailed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during device initialization in controller");
                TempData["ErrorMessage"] = $"Initialization error: {ex.Message}";
                return RedirectToAction("LoginFailed");
            }
        }

        public IActionResult NoDeviceFound()
        {
            return View();
        }

        public IActionResult LoginFailed()
        {
            return View();
        }

        public IActionResult DeviceDisconnected()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AddDevice()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDevice(SystemDevice model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                bool saved = await _deviceService.SaveDeviceCredentialsAsync(model);
                if (saved)
                {
                    // Try to login with new credentials
                    bool loginSuccess = await _deviceService.InitializeAndLoginAsync();
                    if (loginSuccess)
                    {
                        return RedirectToAction("Index", "Attendance");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Credentials saved but login failed. Please check device connectivity.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Failed to save device credentials to database.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding device");
                ModelState.AddModelError("", "An unexpected error occurred while adding the device.");
                return View(model);
            }
        }

        private void OnDeviceDisconnected()
        {
            // This event is raised by the service when SDK detects disconnection
            _logger.LogWarning("Device disconnection detected, you can redirect to disconnected page");

            // You can use TempData to pass message to the disconnected page
            TempData["ErrorMessage"] = "Device connection lost. Attempting to reconnect...";
        }

        private void OnDeviceConnectionStatusChanged(string message)
        {
            // Log connection status changes
            _logger.LogInformation($"Device connection status: {message}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events
                _deviceService.DeviceDisconnected -= OnDeviceDisconnected;
                _deviceService.DeviceConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
            }
            base.Dispose(disposing);
        }
    }
}
