using IntelligentAttendanceSystem.Helper;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    public class FaceManagementController : Controller
    {
        private readonly IFaceManagementService _faceManagementService;
        private readonly ILogger<FaceManagementController> _logger;

        public FaceManagementController(
            IFaceManagementService faceManagementService,
            ILogger<FaceManagementController> logger)
        {
            _faceManagementService = faceManagementService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _faceManagementService.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Countries = Countries.GetCountrySelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FaceUserCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Countries = Countries.GetCountrySelectList();
                return View(request);
            }

            try
            {
                bool success = await _faceManagementService.AddUserAsync(request);
                if (success)
                {
                    TempData["SuccessMessage"] = $"User {request.Name} added successfully";
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Countries = Countries.GetCountrySelectList();
                    ModelState.AddModelError("", "Failed to add user. Please try again.");
                    return View(request);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Countries = Countries.GetCountrySelectList();
                _logger.LogError(ex, "Error creating face user");
                ModelState.AddModelError("", "An error occurred while creating the user.");
                return View(request);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _faceManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var request = new FaceUserUpdateRequest
            {
                Name = user.Name,
                Gender = user.Gender,
                BirthDate = user.BirthDate,
                Department = user.Department,
                Position = user.Position,
                CredentialNumber = user.CredentialNumber
            };

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FaceUserUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                bool success = await _faceManagementService.UpdateUserAsync(id, request);
                if (success)
                {
                    TempData["SuccessMessage"] = $"User {request.Name} updated successfully";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update user. Please try again.");
                    return View(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating face user");
                ModelState.AddModelError("", "An error occurred while updating the user.");
                return View(request);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                bool success = await _faceManagementService.DeleteUserAsync(id);
                return Json(new { success, message = success ? "User deleted successfully" : "Failed to delete user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SyncWithDevice()
        {
            try
            {
                bool success = await _faceManagementService.SyncWithDeviceAsync();
                return Json(new { success, message = success ? "Synchronization completed" : "Synchronization failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing with device");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> Search(string term)
        {
            var users = await _faceManagementService.SearchUsersAsync(term);
            return Json(users.Select(u => new { u.Id, u.Name, u.Department, u.DeviceUserId }));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _faceManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}
