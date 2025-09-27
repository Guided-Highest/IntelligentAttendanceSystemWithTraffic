using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IntelligentAttendanceSystem.Services
{
    public class ShiftService : IShiftService
    {
        private readonly ApplicationDbContext _context;

        public ShiftService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Shift>> GetAllShiftsAsync()
        {
            return await _context.Shifts
                //.Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<bool> UpdateUserShiftAsync(UserShiftViewModel model)
        {
            try
            {
                var userShift = await _context.UserShifts.FindAsync(model.UserShiftId);
                if (userShift == null) return false;

                userShift.ShiftId = model.ShiftId;
                userShift.EffectiveDate = model.EffectiveDate;
                userShift.EndDate = model.EndDate;
                userShift.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<Shift> GetShiftByIdAsync(int shiftId)
        {
            return await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftId == shiftId);
        }

        public async Task<bool> CreateShiftAsync(ShiftViewModel model)
        {
            try
            {
                var shift = new Shift
                {
                    ShiftName = model.ShiftName,
                    ShiftCode = model.ShiftCode,
                    StartTime = model.StartTime,
                    RelaxTime = TimeSpan.FromMinutes(model.RelaxTimeMinutes),
                    OffTime = model.OffTime,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log error
                return false;
            }
        }

        public async Task<bool> UpdateShiftAsync(ShiftViewModel model)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(model.ShiftId);
                if (shift == null) return false;

                shift.ShiftName = model.ShiftName;
                shift.ShiftCode = model.ShiftCode;
                shift.StartTime = model.StartTime;
                shift.RelaxTime = TimeSpan.FromMinutes(model.RelaxTimeMinutes);
                shift.OffTime = model.OffTime;
                shift.Description = model.Description;
                shift.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> DeactivateShiftAsync(int shiftId)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(shiftId);
                if (shift == null) return false;

                shift.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ActivateShiftAsync(int shiftId)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(shiftId);
                if (shift == null) return false;

                shift.IsActive = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<(bool Success, string Message)> DeleteShiftAsync(int shiftId)
        {
            try
            {
                var shift = await _context.Shifts
                    .Include(s => s.UserShifts)
                    .FirstOrDefaultAsync(s => s.ShiftId == shiftId);

                if (shift == null)
                    return (false, "Shift not found.");

                // Check for active assignments
                var activeAssignments = shift.UserShifts?
                    .Where(us => us.IsActive && (us.EndDate == null || us.EndDate >= DateTime.Today))
                    .ToList();

                if (activeAssignments?.Count > 0)
                {
                    var userNames = await _context.Users
                        .Where(u => activeAssignments.Select(a => a.UserId).Contains(u.Id))
                        .Select(u => u.FullName)
                        .ToListAsync();

                    return (false,
                        $"Cannot delete shift. It is currently assigned to {activeAssignments.Count} user(s): " +
                        $"{string.Join(", ", userNames.Take(3))}" +
                        $"{(userNames.Count > 3 ? " and others" : "")}");
                }

                // Check for historical assignments
                var historicalAssignments = shift.UserShifts?
                    .Where(us => us.EffectiveDate < DateTime.Today)
                    .ToList();

                if (historicalAssignments?.Count > 0)
                {
                    // Archive the shift instead of deleting
                    shift.IsActive = false;
                    await _context.SaveChangesAsync();
                    return (true,
                        $"Shift archived due to {historicalAssignments.Count} historical assignment(s). " +
                        "It can be restored if needed.");
                }

                // Safe to delete - no assignments exist
                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();
                return (true, "Shift deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while deleting the shift: {ex.Message}");
            }
        }
        public async Task<List<UserShift>> GetUserShiftsAsync(string userId)
        {
            return await _context.UserShifts
                .Include(us => us.Shift)
                .Where(us => us.UserId == userId)
                .OrderByDescending(us => us.EffectiveDate)
                .ToListAsync();
        }

        public async Task<UserShift> GetCurrentUserShiftAsync(string userId)
        {
            var today = DateTime.Today;
            return await _context.UserShifts
                .Include(us => us.Shift)
                .Where(us => us.UserId == userId &&
                           us.EffectiveDate <= today &&
                           (us.EndDate == null || us.EndDate >= today) &&
                           us.IsActive)
                .OrderByDescending(us => us.EffectiveDate)
                .FirstOrDefaultAsync();
        }
        public async Task<bool> AssignShiftToUserAsync(UserShiftViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // End previous active shift assignment
                var previousShift = await _context.UserShifts
                    .Where(us => us.UserId == model.UserId && us.IsActive &&
                               (us.EndDate == null || us.EndDate >= model.EffectiveDate))
                    .FirstOrDefaultAsync();

                if (previousShift != null)
                {
                    previousShift.EndDate = model.EffectiveDate.AddDays(-1);
                    previousShift.IsActive = false;
                }

                // Create new shift assignment
                var userShift = new UserShift
                {
                    UserId = model.UserId,
                    ShiftId = model.ShiftId,
                    EffectiveDate = model.EffectiveDate,
                    EndDate = model.EndDate,
                    IsActive = true
                };

                _context.UserShifts.Add(userShift);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
        public async Task<bool> AssignShiftToUsersAsync(ShiftAssignmentViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var userId in model.SelectedUserIds)
                {
                    // End previous active shift assignment
                    var previousShift = await _context.UserShifts
                        .Where(us => us.UserId == userId && us.IsActive &&
                                   (us.EndDate == null || us.EndDate >= model.EffectiveDate))
                        .FirstOrDefaultAsync();

                    if (previousShift != null)
                    {
                        previousShift.EndDate = model.EffectiveDate.AddDays(-1);
                        previousShift.IsActive = false;
                    }

                    // Create new shift assignment
                    var userShift = new UserShift
                    {
                        UserId = userId,
                        ShiftId = model.ShiftId,
                        EffectiveDate = model.EffectiveDate,
                        IsActive = true
                    };

                    _context.UserShifts.Add(userShift);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<AttendanceStatus> CalculateAttendanceStatusAsync(string userId, DateTime checkInTime)
        {
            var shift = await GetCurrentUserShiftAsync(userId);
            if (shift == null) return AttendanceStatus.Present; // Default if no shift assigned

            var checkInTimeOnly = checkInTime.TimeOfDay;
            var lateThreshold = shift.Shift.StartTime + shift.Shift.RelaxTime;

            if (checkInTimeOnly <= lateThreshold)
            {
                return AttendanceStatus.Present;
            }
            else
            {
                return AttendanceStatus.Late;
            }
        }

        public async Task<bool> IsCheckInLateAsync(string userId, DateTime checkInTime)
        {
            var status = await CalculateAttendanceStatusAsync(userId, checkInTime);
            return status == AttendanceStatus.Late;
        }

        public async Task<TimeSpan> GetLateDurationAsync(string userId, DateTime checkInTime)
        {
            var shift = await GetCurrentUserShiftAsync(userId);
            if (shift == null) return TimeSpan.Zero;

            var checkInTimeOnly = checkInTime.TimeOfDay;
            var lateThreshold = shift.Shift.StartTime + shift.Shift.RelaxTime;

            if (checkInTimeOnly <= lateThreshold)
            {
                return TimeSpan.Zero;
            }
            else
            {
                return checkInTimeOnly - lateThreshold;
            }
        }
    }
}
