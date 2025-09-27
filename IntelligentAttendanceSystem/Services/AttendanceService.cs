using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IntelligentAttendanceSystem.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Attendance> GetAttendanceAsync(int attendanceId)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);
        }

        public async Task<List<Attendance>> GetUserAttendanceAsync(string userId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Attendances
                .Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value);

            return await query.OrderByDescending(a => a.Date).ToListAsync();
        }

        public async Task<List<AttendanceViewModel>> GetAttendanceByFilterAsync(AttendanceFilterViewModel filter)
        {
            var query = _context.Attendances
                .Include(a => a.User)
                .AsQueryable();

            if (filter.UserType.HasValue)
                query = query.Where(a => a.User.UserType == filter.UserType.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.Date >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.Date <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.SearchString))
            {
                query = query.Where(a => a.User.FullName.Contains(filter.SearchString) ||
                                        a.User.RollNumber.Contains(filter.SearchString) ||
                                        a.User.EmployeeId.Contains(filter.SearchString));
            }

            return await query.Select(a => new AttendanceViewModel
            {
                AttendanceId = a.AttendanceId,
                UserId = a.UserId,
                FullName = a.User.FullName,
                Identifier = a.User.UserType == UserType.Student ? a.User.RollNumber : a.User.EmployeeId,
                Date = a.Date,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                Status = a.Status,
                Remarks = a.Remarks
            }).OrderByDescending(a => a.Date).ThenBy(a => a.FullName).ToListAsync();
        }
        public async Task<bool> MarkAttendanceAsync(AttendanceViewModel model)
        {
            try
            {
                // Calculate status based on shift if user has one
                var shiftService = new ShiftService(_context); // You might want to use dependency injection
                var status = await shiftService.CalculateAttendanceStatusAsync(model.UserId, model.CheckInTime);

                var attendance = new Attendance
                {
                    UserId = model.UserId,
                    Date = model.Date.Date,
                    CheckInTime = model.CheckInTime,
                    CheckOutTime = model.CheckOutTime,
                    Status = status, // Use calculated status
                    Remarks = model.Remarks,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> UpdateAttendanceAsync(AttendanceViewModel model)
        {
            try
            {
                var attendance = await _context.Attendances.FindAsync(model.AttendanceId);
                if (attendance == null) return false;

                attendance.CheckInTime = model.CheckInTime;
                attendance.CheckOutTime = model.CheckOutTime;
                attendance.Status = model.Status;
                attendance.Remarks = model.Remarks;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> MarkBulkAttendanceAsync(BulkAttendanceViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in model.AttendanceItems)
                {
                    var attendance = new Attendance
                    {
                        UserId = item.UserId,
                        Date = model.Date.Date,
                        CheckInTime = item.CheckInTime,
                        Status = item.Status,
                        Remarks = item.Remarks,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Attendances.Add(attendance);
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

        public async Task<bool> CheckOutAsync(int attendanceId, DateTime checkOutTime)
        {
            try
            {
                var attendance = await _context.Attendances.FindAsync(attendanceId);
                if (attendance == null) return false;

                attendance.CheckOutTime = checkOutTime;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<AttendanceReport>> GenerateAttendanceReportAsync(UserType userType, DateTime fromDate, DateTime toDate)
        {
            // Remove HasValue check since userType is not nullable
            var users = await _context.Users
                .Where(u => u.UserType == userType && u.IsActive)
                .ToListAsync();

            var report = new List<AttendanceReport>();

            foreach (var user in users)
            {
                var attendances = await _context.Attendances
                    .Where(a => a.UserId == user.Id && a.Date >= fromDate && a.Date <= toDate)
                    .ToListAsync();

                var totalDays = (toDate - fromDate).Days + 1;
                var presentDays = attendances.Count(a => a.Status == AttendanceStatus.Present);
                var absentDays = totalDays - presentDays; // This is an approximation
                var lateDays = attendances.Count(a => a.Status == AttendanceStatus.Late);

                // Calculate actual absent days (days with no record or status = Absent)
                var actualAbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);
                // For days with no record, consider as absent
                var daysWithRecords = attendances.Select(a => a.Date.Date).Distinct().Count();
                absentDays = actualAbsentDays + (totalDays - daysWithRecords);

                report.Add(new AttendanceReport
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Identifier = userType == UserType.Student ? user.RollNumber : user.EmployeeId,
                    TotalDays = totalDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays,
                    AttendancePercentage = totalDays > 0 ? (decimal)presentDays / totalDays * 100 : 0
                });
            }

            return report.OrderByDescending(r => r.AttendancePercentage).ToList();
        }

        public async Task<Attendance> GetTodaysAttendanceAsync(string userId)
        {
            var today = DateTime.Today;
            return await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);
        }
    }
}
