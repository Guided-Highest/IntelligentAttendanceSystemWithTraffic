using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IShiftService
    {
        Task<List<Shift>> GetAllShiftsAsync();
        Task<Shift> GetShiftByIdAsync(int shiftId);
        Task<bool> CreateShiftAsync(ShiftViewModel model);
        Task<bool> UpdateShiftAsync(ShiftViewModel model);
        Task<bool> DeactivateShiftAsync(int shiftId);
        Task<bool> ActivateShiftAsync(int shiftId);
        Task<(bool Success, string Message)> DeleteShiftAsync(int shiftId);
        Task<List<UserShift>> GetUserShiftsAsync(string userId);
        Task<UserShift> GetCurrentUserShiftAsync(string userId);
        Task<bool> AssignShiftToUserAsync(UserShiftViewModel model);
        Task<bool> AssignShiftToUsersAsync(ShiftAssignmentViewModel model);
        Task<bool> UpdateUserShiftAsync(UserShiftViewModel model);
        Task<AttendanceStatus> CalculateAttendanceStatusAsync(string userId, DateTime checkInTime);
        Task<bool> IsCheckInLateAsync(string userId, DateTime checkInTime);
        Task<TimeSpan> GetLateDurationAsync(string userId, DateTime checkInTime);
    }
}
