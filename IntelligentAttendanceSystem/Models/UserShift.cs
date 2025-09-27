using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligentAttendanceSystem.Models
{
    public class UserShift
    {
        public int UserShiftId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; }
    }
}
