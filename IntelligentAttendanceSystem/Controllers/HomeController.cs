using IntelligentAttendanceSystem.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IntelligentAttendanceSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<HomeController> _logger;
        private readonly IDahuaDeviceService _deviceService;

        public HomeController(IAttendanceService attendanceService, ILogger<HomeController> logger, IDahuaDeviceService deviceService)
        {
            _attendanceService = attendanceService;
            _logger = logger;
            _deviceService = deviceService;
        }

        public IActionResult Index()
        {
            // If device is already initialized and connected, go to attendance
            if (_deviceService.IsInitialized && _deviceService.IsDeviceConnected)
            {
                return RedirectToAction("Index", "FaceRecognition");
            }

            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                // Otherwise, go through device initialization flow
                return RedirectToAction("Initialize", "Device");
            }
            else if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Home");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todayAttendance = await _attendanceService.GetTodaysAttendanceAsync(userId);

            ViewBag.TodayAttendance = todayAttendance;
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}