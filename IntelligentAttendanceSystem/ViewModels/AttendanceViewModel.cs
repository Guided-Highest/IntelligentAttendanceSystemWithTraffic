using IntelligentAttendanceSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class AttendanceViewModel
    {
        public int AttendanceId { get; set; }

        [Required]
        public string UserId { get; set; }

        // Remove [Required] from these as they're for display only
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }  // Remove [Required]

        [Display(Name = "Roll Number/Employee ID")]
        public string? Identifier { get; set; }  // Remove [Required]

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Time)]
        public DateTime CheckInTime { get; set; } = DateTime.Now;

        [DataType(DataType.Time)]
        public DateTime? CheckOutTime { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }

        public string? Remarks { get; set; }
    }

    public class AttendanceFilterViewModel
    {
        public UserType? UserType { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public string? SearchString { get; set; }
    }
    public class AttendanceIndexViewModel
    {
        public List<AttendanceViewModel> Attendances { get; set; }
        public AttendanceFilterViewModel Filter { get; set; }
    }
    public class BulkAttendanceViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public UserType UserType { get; set; }

        public List<BulkAttendanceItem> AttendanceItems { get; set; } = new List<BulkAttendanceItem>();
    }

    public class BulkAttendanceItem
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Identifier { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTime CheckInTime { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 0, 0);
        public string? Remarks { get; set; }
    }
}