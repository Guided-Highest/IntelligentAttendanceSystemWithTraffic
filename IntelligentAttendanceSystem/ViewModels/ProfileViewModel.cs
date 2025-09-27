using IntelligentAttendanceSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public Gender Gender { get; set; }

        [Required]
        [Display(Name = "Birthday")]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Credential Type")]
        public CredentialType CredentialType { get; set; }

        [Required]
        [Display(Name = "Credential No.")]
        public string CredentialNumber { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Region { get; set; }

        [Display(Name = "User Type")]
        public UserType UserType { get; set; }

        // Student specific
        [Display(Name = "Roll Number")]
        public string? RollNumber { get; set; }

        [Display(Name = "Class")]
        public string? Class { get; set; }

        // Staff specific
        [Display(Name = "Employee ID")]
        public string? EmployeeId { get; set; }

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; }

        [Display(Name = "Current Profile Picture")]
        public bool HasProfilePicture { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Account Created")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Current Shift")]
        public string? CurrentShiftName { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
