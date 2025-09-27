using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IAttendanceService
    {
        Task<Attendance> GetAttendanceAsync(int attendanceId);
        Task<List<Attendance>> GetUserAttendanceAsync(string userId, DateTime? fromDate, DateTime? toDate);
        Task<List<AttendanceViewModel>> GetAttendanceByFilterAsync(AttendanceFilterViewModel filter);
        Task<bool> MarkAttendanceAsync(AttendanceViewModel model);
        Task<bool> UpdateAttendanceAsync(AttendanceViewModel model);
        Task<bool> MarkBulkAttendanceAsync(BulkAttendanceViewModel model);
        Task<bool> CheckOutAsync(int attendanceId, DateTime checkOutTime);
        Task<List<AttendanceReport>> GenerateAttendanceReportAsync(UserType userType, DateTime fromDate, DateTime toDate);
        Task<Attendance> GetTodaysAttendanceAsync(string userId);
    }
}
