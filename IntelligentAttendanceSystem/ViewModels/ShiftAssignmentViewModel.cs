using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class ShiftAssignmentViewModel
    {
        [Required]
        [Display(Name = "Shift")]
        public int ShiftId { get; set; }

        [Required]
        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; } = DateTime.Today;

        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}
