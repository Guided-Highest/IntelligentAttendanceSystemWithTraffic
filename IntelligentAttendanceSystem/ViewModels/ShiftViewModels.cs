using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class ShiftViewModel
    {
        public int ShiftId { get; set; }

        [Required]
        [Display(Name = "Shift Name")]
        [StringLength(100)]
        public string ShiftName { get; set; }

        [Required]
        [Display(Name = "Shift Code")]
        [StringLength(10)]
        public string ShiftCode { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Relax Time (Minutes)")]
        [Range(0, 240, ErrorMessage = "Relax time must be between 0 and 240 minutes")]
        public int RelaxTimeMinutes { get; set; } = 15;

        [Required]
        [Display(Name = "Off Time")]
        [DataType(DataType.Time)]
        public TimeSpan OffTime { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Calculated properties
        [Display(Name = "Total Hours")]
        public TimeSpan TotalHours => OffTime - StartTime;

        [Display(Name = "Late Threshold")]
        public TimeSpan LateThreshold => StartTime + TimeSpan.FromMinutes(RelaxTimeMinutes);

        [Display(Name = "Relax Time")]
        public TimeSpan RelaxTime => TimeSpan.FromMinutes(RelaxTimeMinutes);
    }
}
