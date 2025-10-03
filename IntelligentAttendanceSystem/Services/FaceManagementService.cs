using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligentAttendanceSystem.Services
{
    public class FaceManagementService : IFaceManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDahuaDeviceService _deviceService;
        private readonly ILogger<FaceManagementService> _logger;

        public FaceManagementService(
            ApplicationDbContext context,
            IDahuaDeviceService deviceService,
            ILogger<FaceManagementService> logger)
        {
            _context = context;
            _deviceService = deviceService;
            _logger = logger;
        }

        public async Task<List<FaceUser>> GetAllUsersAsync()
        {
            return await _context.FaceUsers
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<FaceUser> GetUserByIdAsync(int id)
        {
            return await _context.FaceUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<FaceUser> GetUserByDeviceIdAsync(string userId)
        {
            return await _context.FaceUsers
                .FirstOrDefaultAsync(u => u.DeviceUserId == userId && u.IsActive);
        }

        public async Task<bool> AddUserAsync(FaceUserCreateRequest request)
        {
            bool IsAdded = false;
            try
            {
                // Generate unique user ID
                string userId = $"U{DateTime.Now:yyyyMMddHHmmssfff}";

                // Convert image to base64
                string faceImageBase64 = null;
                if (request.FaceImage != null && request.FaceImage.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await request.FaceImage.CopyToAsync(memoryStream);
                    faceImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
                }
                var group = _deviceService.GetDefaultGroupInfo();
                var faceUser = new FaceUser
                {
                    DeviceUserId = userId,
                    Name = request.Name,
                    Gender = request.Gender,
                    BirthDate = request.BirthDate,
                    Department = request.Department,
                    Position = request.Position,
                    FaceImageBase64 = faceImageBase64,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    DeviceGroupId = group?.szGroupId,
                    DeviceGroupName = group?.szGroupName,
                    CredentialNumber = request.CredentialNumber,
                    CredentialType = request.CredentialType,
                    Region = request.Region
                };

                _context.FaceUsers.Add(faceUser);
                await _context.SaveChangesAsync();
                IsAdded = await _deviceService.AddUserAsync(request, group);
                // TODO: Add user to device face database
                // This would require SDK methods to add faces to the device
                if (!IsAdded)
                {
                    _context.FaceUsers.Remove(faceUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Face user not added: {request.Name} (ID: {userId})");
                }
                else
                {
                    _logger.LogInformation($"Face user added: {request.Name} (ID: {userId})");
                }
                return IsAdded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding face user");
                return IsAdded;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, FaceUserUpdateRequest request)
        {
            try
            {
                var user = await _context.FaceUsers.FindAsync(id);
                if (user == null) return false;

                user.Name = request.Name;
                user.Gender = request.Gender;
                user.BirthDate = request.BirthDate;
                user.Department = request.Department;
                user.Position = request.Position;
                user.LastUpdated = DateTime.UtcNow;

                if (request.FaceImage != null && request.FaceImage.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await request.FaceImage.CopyToAsync(memoryStream);
                    user.FaceImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
                }

                await _context.SaveChangesAsync();

                // TODO: Update user in device face database

                _logger.LogInformation($"Face user updated: {request.Name} (ID: {user.DeviceUserId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating face user");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.FaceUsers.FindAsync(id);
                if (user == null) return false;

                user.IsActive = false;
                user.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // TODO: Remove user from device face database

                _logger.LogInformation($"Face user deleted: {user.Name} (ID: {user.DeviceUserId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face user");
                return false;
            }
        }

        public async Task<bool> SyncWithDeviceAsync()
        {
            try
            {
                if (!_deviceService.IsDeviceConnected)
                {
                    _logger.LogWarning("Cannot sync with device - device not connected");
                    return false;
                }

                // TODO: Implement device synchronization logic
                // This would involve:
                // 1. Getting user list from device
                // 2. Comparing with local database
                // 3. Adding/updating/deleting users as needed

                _logger.LogInformation("Face database synchronized with device");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing face database with device");
                return false;
            }
        }

        public async Task<List<FaceUser>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllUsersAsync();

            return await _context.FaceUsers
                .Where(u => u.IsActive &&
                           (u.Name.Contains(searchTerm) ||
                            u.DeviceUserId.Contains(searchTerm) ||
                            u.Department.Contains(searchTerm) ||
                            u.Position.Contains(searchTerm) ||
                            u.CredentialNumber.Contains(searchTerm)))
                .OrderBy(u => u.Name)
                .ToListAsync();
        }
    }
}
