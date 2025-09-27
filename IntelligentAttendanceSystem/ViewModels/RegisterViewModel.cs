using IntelligentAttendanceSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public Gender Gender { get; set; }

        [Required]
        [Display(Name = "Birthday")]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; } = DateTime.Now.AddYears(-18);

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

        [Required]
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
    }
}
