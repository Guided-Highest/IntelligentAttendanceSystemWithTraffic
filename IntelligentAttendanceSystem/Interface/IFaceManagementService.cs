using IntelligentAttendanceSystem.Models;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IFaceManagementService
    {
        Task<List<FaceUser>> GetAllUsersAsync();
        Task<FaceUser> GetUserByIdAsync(int id);
        Task<FaceUser> GetUserByDeviceIdAsync(string userId);
        Task<bool> AddUserAsync(FaceUserCreateRequest request);
        Task<bool> UpdateUserAsync(int id, FaceUserUpdateRequest request);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> SyncWithDeviceAsync();
        Task<List<FaceUser>> SearchUsersAsync(string searchTerm);
    }
}
