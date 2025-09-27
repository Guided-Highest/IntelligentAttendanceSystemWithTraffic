using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligentAttendanceSystem.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        [StringLength(100)]
        public string ShiftName { get; set; }

        [Required]
        [StringLength(10)]
        public string ShiftCode { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Relax Time (Grace Period)")]
        public TimeSpan RelaxTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan OffTime { get; set; }

        [Required]
        [Display(Name = "Total Hours")]
        public TimeSpan TotalHours { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties - FIXED: Use UserShifts instead of Users
        public virtual ICollection<UserShift> UserShifts { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }

        // Helper property to calculate late time
        [NotMapped]
        public TimeSpan LateThreshold => StartTime + RelaxTime;

        [NotMapped]
        public string DisplayName => $"{ShiftName} ({StartTime:hh\\:mm} - {OffTime:hh\\:mm})";
    }
}
