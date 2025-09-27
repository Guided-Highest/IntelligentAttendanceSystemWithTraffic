using IntelligentAttendanceSystem.Models;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class ReportViewModel
    {
        public List<AttendanceReport> ReportData { get; set; } = new List<AttendanceReport>();
        public AttendanceFilterViewModel Filter { get; set; } = new AttendanceFilterViewModel();
    }

    public class DashboardViewModel
    {
        public Attendance TodayAttendance { get; set; }
        public int PresentDaysThisMonth { get; set; }
        public int TotalWorkingDaysThisMonth { get; set; }
        public decimal MonthlyAttendancePercentage { get; set; }
    }
}
