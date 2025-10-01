using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.Services;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IntelligentAttendanceSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceController> _logger;
        private readonly IDahuaDeviceService _deviceService;

        public AttendanceController(IAttendanceService attendanceService, ApplicationDbContext context, ILogger<AttendanceController> logger, IDahuaDeviceService deviceService)
        {
            _attendanceService = attendanceService;
            _context = context;
            _logger = logger;
            _deviceService = deviceService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(AttendanceFilterViewModel filter)
        {
            if (!_deviceService.IsDeviceConnected)
            {
                return RedirectToAction("DeviceDisconnected", "Device");
            }
            filter.FromDate ??= DateTime.Today.AddDays(-30);
            filter.ToDate ??= DateTime.Today;

            var attendances = await _attendanceService.GetAttendanceByFilterAsync(filter);
            ViewData["Filter"] = filter;
            var viewModel = new AttendanceIndexViewModel
            {
                Attendances = attendances,
                Filter = filter
            };
            return View(viewModel);
        }

        public async Task<IActionResult> MyAttendance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var attendances = await _attendanceService.GetUserAttendanceAsync(userId, DateTime.Today.AddDays(-30), DateTime.Today);
            return View(attendances);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult MarkIndividual()
        {
            var model = new AttendanceViewModel();
            PopulateUserList();
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkIndividual(AttendanceViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _attendanceService.MarkAttendanceAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Attendance marked successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Error marking attendance. Please try again.");
            }
            PopulateUserList();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkBulk()
        {
            var model = new BulkAttendanceViewModel
            {
                Date = DateTime.Today
            };
            await PopulateBulkAttendanceItems(model);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkBulk(BulkAttendanceViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _attendanceService.MarkBulkAttendanceAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Bulk attendance marked successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Error marking bulk attendance. Please try again.");
            }
            await PopulateBulkAttendanceItems(model);
            return View(model);
        }

        public async Task<IActionResult> CheckIn()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todayAttendance = await _attendanceService.GetTodaysAttendanceAsync(userId);

            if (todayAttendance != null)
            {
                TempData["InfoMessage"] = "You have already checked in today.";
                return RedirectToAction(nameof(MyAttendance));
            }

            var model = new AttendanceViewModel
            {
                UserId = userId,
                Date = DateTime.Today,
                CheckInTime = DateTime.Now,
                Status = AttendanceStatus.Present
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(AttendanceViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Remove errors for fields not in the form
                ModelState.Remove("FullName");
                ModelState.Remove("Identifier");

                // Calculate status based on shift
                var shiftService = new ShiftService(_context);
                model.Status = await shiftService.CalculateAttendanceStatusAsync(model.UserId, model.CheckInTime);

                // Check if user is late
                if (model.Status == AttendanceStatus.Late)
                {
                    var lateDuration = await shiftService.GetLateDurationAsync(model.UserId, model.CheckInTime);
                    model.Remarks = $"Late by {lateDuration:mm\\:ss} minutes. {model.Remarks}";
                }

                var result = await _attendanceService.MarkAttendanceAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = model.Status == AttendanceStatus.Late ?
                        "Checked in successfully (Late)" : "Checked in successfully!";
                    return RedirectToAction(nameof(MyAttendance));
                }
                ModelState.AddModelError("", "Error checking in. Please try again.");
            }

            return View(model);
        }
        public async Task<IActionResult> CheckOut()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todayAttendance = await _attendanceService.GetTodaysAttendanceAsync(userId);

            if (todayAttendance == null || todayAttendance.CheckOutTime.HasValue)
            {
                TempData["InfoMessage"] = "No check-in found or already checked out for today.";
                return RedirectToAction(nameof(MyAttendance));
            }

            return View(todayAttendance);
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut(int attendanceId)
        {
            var result = await _attendanceService.CheckOutAsync(attendanceId, DateTime.Now);
            if (result)
            {
                TempData["SuccessMessage"] = "Checked out successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error checking out. Please try again.";
            }
            return RedirectToAction(nameof(MyAttendance));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reports()
        {
            var model = new ReportViewModel
            {
                Filter = new AttendanceFilterViewModel
                {
                    FromDate = DateTime.Today.AddDays(-30),
                    ToDate = DateTime.Today
                }
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateReport(AttendanceFilterViewModel filter)
        {
            // Fix the HasValue check - use direct null check for nullable enum
            if (filter.UserType == null) // Instead of !filter.UserType.HasValue
            {
                ModelState.AddModelError("UserType", "Please select a user type.");

                // Return to view with current data
                var attendances = await _attendanceService.GetAttendanceByFilterAsync(filter);
                var viewModel = new AttendanceIndexViewModel
                {
                    Attendances = attendances,
                    Filter = filter
                };
                return View("Reports", viewModel);
            }

            var report = await _attendanceService.GenerateAttendanceReportAsync(
                filter.UserType.Value, filter.FromDate.Value, filter.ToDate.Value);

            // Create a proper ViewModel instead of using ViewBag
            var reportViewModel = new ReportViewModel
            {
                ReportData = report,
                Filter = filter
            };

            return View("Reports", reportViewModel);
        }
        private void PopulateUserList()
        {
            var users = _context.Users.Where(u => u.IsActive).ToList();
            ViewBag.Users = users.Select(u => new
            {
                UserId = u.Id,
                DisplayName = $"{u.FullName} ({(u.UserType == UserType.Student ? u.RollNumber : u.EmployeeId)})"
            });
        }

        private async Task PopulateBulkAttendanceItems(BulkAttendanceViewModel model)
        {
            var users = await _context.Users
                .Where(u => u.UserType == model.UserType && u.IsActive)
                .ToListAsync();

            model.AttendanceItems = users.Select(u => new BulkAttendanceItem
            {
                UserId = u.Id,
                FullName = u.FullName,
                Identifier = model.UserType == UserType.Student ? u.RollNumber : u.EmployeeId,
                Status = AttendanceStatus.Present
            }).ToList();
        }
    }
}