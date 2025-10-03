using System.ComponentModel.DataAnnotations;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Models
{
    public class FaceUser
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? DeviceUserId { get; set; } // Device user ID
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string FaceImageBase64 { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsActive { get; set; } = true;
        public string DeviceGroupId { get; set; }
        public string DeviceGroupName { get; set; }

        [Display(Name = "Credential Type")]
        public CredentialType CredentialType { get; set; }
        [Display(Name = "Credential No.")]
        public string CredentialNumber { get; set; }
        [Display(Name = "Country")]
        public string Region { get; set; }
        [Required]
        public UserType UserType { get; set; }
        // Navigation property to attendance records
        public virtual ICollection<FaceAttendanceRecord> AttendanceRecords { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
    }

    public class FaceUserCreateRequest
    {
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public IFormFile FaceImage { get; set; }
        public CredentialType CredentialType { get; set; }
        public string CredentialNumber { get; set; }
        public string Region { get; set; }
    }

    public class FaceUserUpdateRequest
    {
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public IFormFile FaceImage { get; set; }
        public CredentialType CredentialType { get; set; }
        public string CredentialNumber { get; set; }
        public string Region { get; set; }
    }
}
