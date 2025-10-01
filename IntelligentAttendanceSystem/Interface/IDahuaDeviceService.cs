using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;
using NetSDKCS;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IDahuaDeviceService : IDisposable
    {
        Task<bool> InitializeAndLoginAsync();
        Task<SystemDevice> GetDeviceCredentialsAsync();
        Task<bool> SaveDeviceCredentialsAsync(SystemDevice credentials);
        Task<bool> AddUserAsync(FaceUserCreateRequest model, NET_FACERECONGNITION_GROUP_INFO? groupInfo);
        NET_FACERECONGNITION_GROUP_INFO? GetDefaultGroupInfo();
        // Properties
        bool IsDeviceConnected { get; }
        bool IsInitialized { get; }
        string InitializationError { get; }
        Exception InitializationException { get; }
        IntPtr LoginID { get; }
        NET_DEVICEINFO_Ex DeviceInfo { get; }
        string DeviceIP { get; }

        // Events
        event Action DeviceDisconnected;
        event Action<string> DeviceConnectionStatusChanged;
    }
}
