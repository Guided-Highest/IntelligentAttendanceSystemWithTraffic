using IntelligentAttendanceSystem.Models;

namespace IntelligentAttendanceSystem.Helper
{
    public static class GlobalHelper
    {
        // Helper method to get current shift name
        public static string GetCurrentShiftName(this ApplicationUser user)
        {
            var currentShift = user.UserShifts?
                .Where(us => us.IsActive &&
                       us.EffectiveDate <= DateTime.Today &&
                       (us.EndDate == null || us.EndDate >= DateTime.Today))
                .OrderByDescending(us => us.EffectiveDate)
                .Select(us => us.Shift?.ShiftName)
                .FirstOrDefault();

            return currentShift;
        }
        public enum ShiftMode
        {
            simple, Efficient, Simpler
        }
    }
}
