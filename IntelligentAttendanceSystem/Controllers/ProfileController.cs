using IntelligentAttendanceSystem.Helper;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserManager<ApplicationUser> userManager,
                               ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Gender = user.Gender,
                Birthday = user.Birthday,
                Address = user.Address,
                CredentialType = user.CredentialType,
                CredentialNumber = user.CredentialNumber,
                Region = user.Region,
                RollNumber = user.RollNumber,
                Class = user.Class,
                EmployeeId = user.EmployeeId,
                Department = user.Department,
                HasProfilePicture = user.ProfilePicture != null,
                PhoneNumber = user.PhoneNumber,
                CreatedDate = user.CreatedDate,
                UpdatedDate = user.UpdatedDate,
                CurrentShiftName = user.CurrentShift?.ShiftName
            };

            ViewBag.Countries = Countries.GetCountrySelectList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Countries = Countries.GetCountrySelectList();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            try
            {
                // Handle profile picture upload
                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    var uploadResult = await ProcessProfilePicture(model.ProfilePictureFile);
                    if (uploadResult.Success)
                    {
                        user.ProfilePicture = uploadResult.ImageData;
                        user.ProfilePictureContentType = uploadResult.ContentType;
                        user.ProfilePictureSize = uploadResult.FileSize;
                    }
                    else
                    {
                        ModelState.AddModelError("ProfilePictureFile", uploadResult.ErrorMessage);
                        ViewBag.Countries = Countries.GetCountrySelectList();
                        return View(model);
                    }
                }

                // Update user properties
                user.FullName = model.FullName;
                user.Gender = model.Gender;
                user.Birthday = model.Birthday;
                user.Address = model.Address;
                user.CredentialType = model.CredentialType;
                user.CredentialNumber = model.CredentialNumber;
                user.Region = model.Region;
                user.PhoneNumber = model.PhoneNumber;
                user.UpdatedDate = DateTime.UtcNow;

                // Update user type specific fields
                user.RollNumber = model.RollNumber;
                user.Class = model.Class;
                user.EmployeeId = model.EmployeeId;
                user.Department = model.Department;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User profile updated successfully.");
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile.");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the profile.");
            }

            ViewBag.Countries = Countries.GetCountrySelectList();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Picture(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.ProfilePicture == null)
            {
                // Return default avatar
                var defaultAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "default-avatar.png");
                if (System.IO.File.Exists(defaultAvatarPath))
                {
                    return PhysicalFile(defaultAvatarPath, "image/png");
                }
                return NotFound();
            }

            return File(user.ProfilePicture, user.ProfilePictureContentType ?? "image/jpeg");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.ProfilePicture = null;
            user.ProfilePictureContentType = null;
            user.ProfilePictureSize = null;
            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile picture removed successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove profile picture.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<(bool Success, byte[] ImageData, string ContentType, long FileSize, string ErrorMessage)>
            ProcessProfilePicture(IFormFile file)
        {
            try
            {
                // Validate file size (5MB limit)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return (false, null, null, 0, "File size must be less than 5MB.");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif" };

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension) ||
                    !allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    return (false, null, null, 0, "Only JPG, JPEG, PNG, and GIF files are allowed.");
                }

                // Resize image to optimize storage (optional)
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                // You can add image resizing logic here if needed
                var imageData = ImageHelper.CompressImage(memoryStream.ToArray());

                return (true, imageData, file.ContentType, file.Length, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing profile picture.");
                return (false, null, null, 0, "An error occurred while processing the image.");
            }
        }

        // Keep the ChangePassword action as before
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            _logger.LogInformation("User changed their password successfully.");
            TempData["SuccessMessage"] = "Your password has been changed successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
