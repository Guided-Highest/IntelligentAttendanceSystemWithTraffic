using IntelligentAttendanceSystem.Helper;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligentAttendanceSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        public UserType UserType { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; } = DateTime.Now.AddYears(-18);

        [Required]
        [StringLength(500)]
        public string Address { get; set; }

        [Required]
        public CredentialType CredentialType { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Credential Number")]
        public string CredentialNumber { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        [Display(Name = "Country")]
        public string Region { get; set; }

        // Profile Picture stored in database
        public byte[]? ProfilePicture { get; set; }

        [StringLength(50)]
        public string? ProfilePictureContentType { get; set; }

        public long? ProfilePictureSize { get; set; }

        // For Students
        public string? RollNumber { get; set; }
        public string? Class { get; set; }

        // For Staff
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }

        // Use UserShifts for shift history
        public virtual ICollection<UserShift> UserShifts { get; set; }

        // Helper property to get current shift
        [NotMapped]
        public Shift? CurrentShift => UserShifts?
            .Where(us => us.IsActive &&
                   us.EffectiveDate <= DateTime.Today &&
                   (us.EndDate == null || us.EndDate >= DateTime.Today))
            .OrderByDescending(us => us.EffectiveDate)
            .Select(us => us.Shift)
            .FirstOrDefault();

        [NotMapped]
        public int? CurrentShiftId => CurrentShift?.ShiftId;

        [NotMapped]
        public string CountryName => Countries.GetCountryName(Region);

        [NotMapped]
        public string ProfilePictureUrl => ProfilePicture != null
       ? $"/Profile/Picture/{Id}"
       : "/images/default-avatar.png";

        [NotMapped]
        public string ProfilePictureBase64 => ProfilePicture != null
            ? $"data:{ProfilePictureContentType};base64,{Convert.ToBase64String(ProfilePicture)}"
            : null;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        public virtual ICollection<Attendance> Attendances { get; set; }

        public ApplicationUser()
        {
            UserShifts = new HashSet<UserShift>();
            Attendances = new HashSet<Attendance>();
        }
    }

    public enum UserType
    {
        Student,
        Staff,
        Admin
    }
    public enum Gender
    {
        Male,
        Female
    }
    public enum CredentialType
    {
        [Display(Name = "ID Card")]
        IDCard,
        [Display(Name = "Passport")]
        Passport,
        [Display(Name = "Officer Card")]
        DriverLicense
    }
}
