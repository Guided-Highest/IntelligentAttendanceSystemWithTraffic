using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Helper;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static IntelligentAttendanceSystem.Helper.GlobalHelper;

namespace IntelligentAttendanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShiftController : Controller
    {
        private readonly IShiftService _shiftService;
        private readonly ApplicationDbContext _context;

        public ShiftController(IShiftService shiftService, ApplicationDbContext context)
        {
            _shiftService = shiftService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            return View(shifts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new ShiftViewModel
            {
                StartTime = new TimeSpan(9, 0, 0), // Default 9:00 AM
                OffTime = new TimeSpan(17, 0, 0),  // Default 5:00 PM
                RelaxTimeMinutes = 15              // Default 15 minutes grace period
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShiftViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate that OffTime is after StartTime
                if (model.OffTime <= model.StartTime)
                {
                    ModelState.AddModelError("OffTime", "Off Time must be after Start Time.");
                    return View(model);
                }

                var result = await _shiftService.CreateShiftAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Shift created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Error creating shift. Please try again.");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var shift = await _shiftService.GetShiftByIdAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            var model = new ShiftViewModel
            {
                ShiftId = shift.ShiftId,
                ShiftName = shift.ShiftName,
                ShiftCode = shift.ShiftCode,
                StartTime = shift.StartTime,
                RelaxTimeMinutes = (int)shift.RelaxTime.TotalMinutes,
                OffTime = shift.OffTime,
                Description = shift.Description,
                IsActive = shift.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShiftViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.OffTime <= model.StartTime)
                {
                    ModelState.AddModelError("OffTime", "Off Time must be after Start Time.");
                    return View(model);
                }

                var result = await _shiftService.UpdateShiftAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Shift updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Error updating shift. Please try again.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Use the enhanced version that returns a tuple
            var (success, message) = await _shiftService.DeleteShiftAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AssignShift()
        {
            var model = new ShiftAssignmentViewModel
            {
                EffectiveDate = DateTime.Today
            };
            await PopulateShiftAssignmentData(model, ShiftMode.simple);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignShift(ShiftAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.SelectedUserIds == null || !model.SelectedUserIds.Any())
                {
                    ModelState.AddModelError("", "Please select at least one user.");
                    await PopulateShiftAssignmentData(model, ShiftMode.simple);
                    return View(model);
                }

                var result = await _shiftService.AssignShiftToUsersAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Shift assigned to selected users successfully!";
                    return RedirectToAction(nameof(ShiftAssignments));
                }
                ModelState.AddModelError("", "Error assigning shift. Please try again.");
            }

            await PopulateShiftAssignmentData(model, ShiftMode.simple);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ShiftAssignments()
        {
            var userShifts = await _context.UserShifts
                .Include(us => us.User)
                .Include(us => us.Shift)
                .Where(us => us.IsActive)
                .OrderByDescending(us => us.EffectiveDate)
                .ThenBy(us => us.User.FullName)
                .ToListAsync();

            return View(userShifts);
        }

        [HttpGet]
        public async Task<IActionResult> MyShift()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentShift = await _shiftService.GetCurrentUserShiftAsync(userId);

            if (currentShift == null)
            {
                ViewBag.Message = "No shift assigned to you currently.";
            }

            return View(currentShift);
        }
        [HttpGet]
        public async Task<IActionResult> EditAssignment(int id)
        {
            var userShift = await _context.UserShifts
                .Include(us => us.User)
                .Include(us => us.Shift)
                .FirstOrDefaultAsync(us => us.UserShiftId == id);

            if (userShift == null)
            {
                return NotFound();
            }

            var model = new UserShiftViewModel
            {
                UserShiftId = userShift.UserShiftId,
                UserId = userShift.UserId,
                UserName = userShift.User.FullName,
                ShiftId = userShift.ShiftId,
                ShiftName = userShift.Shift.ShiftName,
                EffectiveDate = userShift.EffectiveDate,
                EndDate = userShift.EndDate,
                IsActive = userShift.IsActive
            };

            await PopulateShiftAssignmentData();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(UserShiftViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _shiftService.UpdateUserShiftAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Shift assignment updated successfully!";
                    return RedirectToAction(nameof(ShiftAssignments));
                }
                ModelState.AddModelError("", "Error updating shift assignment. Please try again.");
            }

            await PopulateShiftAssignmentData();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            try
            {
                var userShift = await _context.UserShifts
                    .Include(us => us.User)
                    .Include(us => us.Shift)
                    .FirstOrDefaultAsync(us => us.UserShiftId == id);

                if (userShift == null)
                {
                    TempData["ErrorMessage"] = "Shift assignment not found.";
                    return RedirectToAction(nameof(ShiftAssignments));
                }

                // Check if this is the user's current active assignment
                var isCurrentAssignment = userShift.IsActive &&
                    userShift.EffectiveDate <= DateTime.Today &&
                    (userShift.EndDate == null || userShift.EndDate >= DateTime.Today);

                _context.UserShifts.Remove(userShift);
                await _context.SaveChangesAsync();

                if (isCurrentAssignment)
                {
                    TempData["WarningMessage"] = $"Shift assignment for {userShift.User.FullName} has been removed. They no longer have an active shift.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Shift assignment deleted successfully!";
                }

                return RedirectToAction(nameof(ShiftAssignments));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting shift assignment: {ex.Message}";
                return RedirectToAction(nameof(ShiftAssignments));
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserShiftHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserShifts)
                    .ThenInclude(us => us.Shift)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserName = user.FullName;
            return View(user.UserShifts.OrderByDescending(us => us.EffectiveDate).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndAssignment(int id)
        {
            try
            {
                var userShift = await _context.UserShifts
                    .Include(us => us.User)
                    .FirstOrDefaultAsync(us => us.UserShiftId == id);

                if (userShift == null)
                {
                    TempData["ErrorMessage"] = "Shift assignment not found.";
                    return RedirectToAction(nameof(ShiftAssignments));
                }

                // Set end date to today and deactivate
                userShift.EndDate = DateTime.Today;
                userShift.IsActive = false;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Shift assignment for {userShift.User.FullName} ended successfully.";
                return RedirectToAction(nameof(ShiftAssignments));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error ending shift assignment: {ex.Message}";
                return RedirectToAction(nameof(ShiftAssignments));
            }
        }

        private async Task PopulateShiftAssignmentData()
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            ViewBag.Shifts = shifts;

            var users = await _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.UserShifts)
                    .ThenInclude(us => us.Shift)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Users = users.Select(u => new
            {
                UserId = u.Id,
                DisplayName = $"{u.FullName} () - Current Shift: {u.CurrentShift?.ShiftName ?? "Not Assigned"}"
            });
        }
        private async Task PopulateShiftAssignmentData(ShiftAssignmentViewModel model, ShiftMode mode)
        {
            switch (mode)
            {
                case ShiftMode.simple:
                    await PsadSimple(model);
                    break;
                case ShiftMode.Efficient:
                    await PsadEfficient(model);
                    break;
                case ShiftMode.Simpler:
                    await PsadSimpler(model);
                    break;
            }
        }
        private async Task PsadSimple(ShiftAssignmentViewModel model)
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            ViewBag.Shifts = shifts;

            // Get users with their current shift information using the UserShifts relationship
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.UserShifts)  // Include UserShifts
                    .ThenInclude(us => us.Shift)  // Then include Shift from UserShifts
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Users = users.Select(u => new
            {
                UserId = u.Id,
                DisplayName = $"{u.FullName} () - Current Shift: {GlobalHelper.GetCurrentShiftName(u) ?? "Not Assigned"}"
            });
        }
        private async Task PsadEfficient(ShiftAssignmentViewModel model)
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            ViewBag.Shifts = shifts;

            // More efficient query using Join
            var usersWithShifts = await (from u in _context.Users
                                         join us in _context.UserShifts on u.Id equals us.UserId into userShifts
                                         from us in userShifts
                                             .Where(us => us.IsActive &&
                                                    us.EffectiveDate <= DateTime.Today &&
                                                    (us.EndDate == null || us.EndDate >= DateTime.Today))
                                             .OrderByDescending(us => us.EffectiveDate)
                                             .Take(1)
                                             .DefaultIfEmpty()
                                         join s in _context.Shifts on us.ShiftId equals s.ShiftId into shiftsJoin
                                         from s in shiftsJoin.DefaultIfEmpty()
                                         where u.IsActive
                                         orderby u.FullName
                                         select new
                                         {
                                             UserId = u.Id,
                                             u.FullName,
                                             CurrentShiftName = s != null ? s.ShiftName : "Not Assigned"
                                         }).ToListAsync();

            ViewBag.Users = usersWithShifts.Select(u => new
            {
                u.UserId,
                DisplayName = $"{u.FullName} () - Current Shift: {u.CurrentShiftName}"
            });
        }
        private async Task PsadSimpler(ShiftAssignmentViewModel model)
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            ViewBag.Shifts = shifts;

            // Load users with their shifts for the CurrentShift property to work
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.UserShifts)  // Include UserShifts
                    .ThenInclude(us => us.Shift)  // Then include Shift
                .OrderBy(u => u.FullName)
                .AsSplitQuery()  // Optional: to avoid Cartesian product issues
                .ToListAsync();

            ViewBag.Users = users.Select(u => new
            {
                UserId = u.Id,
                DisplayName = $"{u.FullName} () - Current Shift: {u.CurrentShift?.ShiftName ?? "Not Assigned"}"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _shiftService.DeactivateShiftAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Shift deactivated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deactivating shift.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _shiftService.ActivateShiftAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Shift activated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error activating shift.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}