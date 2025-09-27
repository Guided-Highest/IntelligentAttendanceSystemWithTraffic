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

        public HomeController(IAttendanceService attendanceService, ILogger<HomeController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
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