using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Extensions;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using NetSDKCS;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace IntelligentAttendanceSystem.Services
{
    public class DahuaDeviceService : IDahuaDeviceService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DahuaDeviceService> _logger;
        private IntPtr m_LoginID = IntPtr.Zero;

        // Critical: Store the delegate to prevent garbage collection
        private fDisConnectCallBack m_DisConnectCallBack;
        private static bool s_SdkInitialized = false;
        private static readonly object s_initLock = new object();

        // Public properties to expose internal state
        public IntPtr LoginID => m_LoginID;
        public NET_DEVICEINFO_Ex DeviceInfo { get; private set; }
        public bool IsDeviceConnected { get; private set; }
        public string DeviceIP { get; private set; }

        public event Action DeviceDisconnected;
        public event Action<string> DeviceConnectionStatusChanged;


        public bool IsInitialized { get; private set; }
        public string InitializationError { get; private set; }
        public Exception InitializationException { get; private set; }


        private const int m_WaitTime = 3000;
        private NET_FACERECONGNITION_GROUP_INFO[] m_Groups;


        private readonly SemaphoreSlim _loginSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _deviceOperationSemaphore = new SemaphoreSlim(1, 1);


        public DahuaDeviceService(
        IServiceScopeFactory scopeFactory,
        ILogger<DahuaDeviceService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            m_DisConnectCallBack = new fDisConnectCallBack(DisConnectCallBack);
        }
        public async Task<bool> InitializeAndLoginAsync()
        {
            try
            {
                var credentials = await GetDeviceCredentialsAsync();
                if (credentials == null)
                {
                    _logger.LogWarning("No device credentials found in database");
                    InitializationError = "No device credentials found in database";
                    IsInitialized = false;
                    return false;
                }

                bool loginSuccess = await LoginToDeviceAsync(credentials);

                IsInitialized = loginSuccess;
                if (!loginSuccess)
                {
                    InitializationError = "Device login failed. Check credentials and network connectivity.";
                }

                return loginSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize and login to device");
                InitializationError = $"Initialization failed: {ex.Message}";
                InitializationException = ex;
                IsInitialized = false;
                return false;
            }
        }

        private bool InitializeSdk()
        {
            lock (s_initLock)
            {
                if (!s_SdkInitialized)
                {
                    try
                    {
                        if (!NETClient.Init(m_DisConnectCallBack, IntPtr.Zero, null))
                        {
                            _logger.LogError("Dahua NETClient SDK initialization failed");
                            return false;
                        }
                        s_SdkInitialized = true;
                        _logger.LogInformation("Dahua NETClient SDK initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception during SDK initialization");
                        return false;
                    }
                }
                return true;
            }
        }

        private async Task<bool> LoginToDeviceAsync(SystemDevice credentials)
        {
            await _loginSemaphore.WaitAsync();
            try
            {
                // If already connected, return true
                if (IsDeviceConnected && LoginID != IntPtr.Zero)
                {
                    return true;
                }
                if (!InitializeSdk())
                    return false;

                try
                {
                    // Create local variable for ref parameter
                    NET_DEVICEINFO_Ex deviceInfoLocal = new NET_DEVICEINFO_Ex();

                    m_LoginID = NETClient.LoginWithHighLevelSecurity(
                        credentials.IPAddress,
                        (ushort)credentials.Port,
                        credentials.UserName,
                        credentials.Password,
                        EM_LOGIN_SPAC_CAP_TYPE.TCP,
                        IntPtr.Zero,
                        ref deviceInfoLocal
                    );

                    if (m_LoginID == IntPtr.Zero || m_LoginID == new IntPtr(-1))
                    {
                        string errorCode = NETClient.GetLastError(); // If available in your SDK
                        _logger.LogError($"Dahua device login failed for {credentials.IPAddress}. Error code: {errorCode}");
                        return false;
                    }

                    // Store the results in properties
                    this.DeviceInfo = deviceInfoLocal;
                    this.DeviceIP = credentials.IPAddress;
                    this.IsDeviceConnected = true;

                    _logger.LogInformation($"Successfully logged into Dahua device at {credentials.IPAddress}");
                    _logger.LogInformation($"Login ID: {m_LoginID}, Device Channels: {deviceInfoLocal.nChanNum}");

                    OnDeviceConnectionStatusChanged("Device connected and logged in");
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var faceRecognitionService = scope.ServiceProvider.GetRequiredService<IFaceRecognitionService>();
                        bool success = await faceRecognitionService.StartFaceRecognitionAsync();
                        if (success)
                        {
                            _logger.LogInformation($"Face recognition started on Start");
                        }
                        else
                        {
                            _logger.LogError($"Failed to start face recognition on Start");
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception during login to device {credentials.IPAddress}");
                    return false;
                }
            }
            finally
            {
                _loginSemaphore.Release();
            }
        }

        // Disconnection callback - called by SDK when device disconnects
        private void DisConnectCallBack(IntPtr lLoginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {
            var deviceIp = Marshal.PtrToStringAnsi(pchDVRIP);
            _logger.LogWarning($"Device disconnected: {deviceIp}:{nDVRPort}");

            // Clean up resources
            CleanupResources();

            // Update status and notify
            IsDeviceConnected = false;
            OnDeviceConnectionStatusChanged($"Device disconnected: {deviceIp}");
            OnDeviceDisconnected();
        }

        private void CleanupResources()
        {
            try
            {
                if (m_LoginID != IntPtr.Zero && m_LoginID != new IntPtr(-1))
                {
                    NETClient.Logout(m_LoginID);
                    m_LoginID = IntPtr.Zero;
                }

                // Clear device info
                this.DeviceInfo = default(NET_DEVICEINFO_Ex);
                this.DeviceIP = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resource cleanup");
            }
        }

        private void OnDeviceDisconnected()
        {
            try
            {
                DeviceDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeviceDisconnected event handlers");
            }
        }

        private void OnDeviceConnectionStatusChanged(string message)
        {
            try
            {
                DeviceConnectionStatusChanged?.Invoke(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeviceConnectionStatusChanged event handlers");
            }
        }

        public async Task<SystemDevice> GetDeviceCredentialsAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await context.SystemDevices
                    .Where(d => d.IsActive)
                    .OrderByDescending(d => d.CreatedDate)
                    .FirstOrDefaultAsync();
            }
        }

        public async Task<bool> SaveDeviceCredentialsAsync(SystemDevice credentials)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    var existing = await context.SystemDevices
                        .Where(d => d.IsActive)
                        .OrderByDescending(d => d.CreatedDate)
                        .FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        existing.IsActive = false;
                        context.SystemDevices.Update(existing);
                    }

                    credentials.CreatedDate = DateTime.UtcNow;
                    credentials.IsActive = true;
                    credentials.DeviceType = "";
                    credentials.DetailType = "";
                    credentials.Status = "";
                    context.SystemDevices.Add(credentials);
                    await context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving device credentials");
                    return false;
                }
            }
        }

        // Additional device operations
        public async Task<List<UserInfo>> GetDeviceUsersAsync()
        {
            if (!IsDeviceConnected || LoginID == IntPtr.Zero)
            {
                throw new InvalidOperationException("Device is not connected");
            }

            try
            {
                var users = new List<UserInfo>();
                // Use NETClient methods to get user list
                // Example: NETClient.GetUserList(LoginID, ...);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device users");
                throw;
            }
        }

        public async Task<DeviceStatus> GetDeviceStatusAsync()
        {
            return new DeviceStatus
            {
                IsConnected = this.IsDeviceConnected,
                LoginID = this.LoginID,
                DeviceIP = this.DeviceIP,
                DeviceInfo = this.DeviceInfo,
                LastConnected = DateTime.Now
            };
        }

        public void Dispose()
        {
            CleanupResources();

            if (s_SdkInitialized)
            {
                try
                {
                    NETClient.Cleanup();
                    s_SdkInitialized = false;
                    _logger.LogInformation("Dahua SDK cleaned up successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during SDK cleanup");
                }
            }
        }

        public async Task<bool> AddUserAsync(FaceUserCreateRequest model, NET_FACERECONGNITION_GROUP_INFO? groupInfo)
        {

            if (!IsDeviceConnected || LoginID == IntPtr.Zero)
            {
                throw new InvalidOperationException("Device is not connected");
            }
            var picture = model.FaceImage?.OpenReadStream().ReadFully();
            await _deviceOperationSemaphore.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    bool ret = false;
                    NET_IN_OPERATE_FACERECONGNITIONDB stuInParam = new NET_IN_OPERATE_FACERECONGNITIONDB();
                    IntPtr groupIdPtr = IntPtr.Zero;
                    IntPtr groupNamePtr = IntPtr.Zero;
                    IntPtr pictureBufferPtr = IntPtr.Zero;

                    try
                    {
                        // Get group info - you'll need to implement this method
                        if (groupInfo == null)
                        {
                            _logger.LogError("No default group found for device");
                            return false;
                        }

                        stuInParam.dwSize = (uint)Marshal.SizeOf(typeof(NET_IN_OPERATE_FACERECONGNITIONDB));
                        stuInParam.emOperateType = EM_OPERATE_FACERECONGNITIONDB_TYPE.ADD;
                        stuInParam.stPersonInfo.szPersonNameEx = model.Name.Trim();
                        stuInParam.stPersonInfo.szID = model.CredentialNumber.Trim();
                        stuInParam.stPersonInfo.szCountry = model.Region;
                        stuInParam.stPersonInfo.bySex = (byte)((int)model.Gender + 1);

                        // Marshal group information
                        groupIdPtr = Marshal.StringToHGlobalAnsi(groupInfo.Value.szGroupId);
                        groupNamePtr = Marshal.StringToHGlobalAnsi(groupInfo.Value.szGroupName);

                        stuInParam.stPersonInfo.pszGroupID = groupIdPtr;
                        stuInParam.stPersonInfo.bGroupIdLen = (byte)groupInfo.Value.szGroupId.Length;
                        stuInParam.stPersonInfo.pszGroupName = groupNamePtr;
                        stuInParam.stPersonInfo.byIDType = (byte)((int)model.CredentialType + 1);
                        stuInParam.stPersonInfo.wYear = (ushort)model.BirthDate?.Year;
                        stuInParam.stPersonInfo.byMonth = (byte)model.BirthDate?.Month;
                        stuInParam.stPersonInfo.byDay = (byte)model.BirthDate?.Day;

                        if (picture != null && picture.Length > 0)
                        {
                            stuInParam.stPersonInfo.wFacePicNum = 1;
                            stuInParam.stPersonInfo.szFacePicInfo = new NET_PIC_INFO[48];
                            for (int i = 0; i < 48; i++)
                            {
                                stuInParam.stPersonInfo.szFacePicInfo[i] = new NET_PIC_INFO();
                            }

                            stuInParam.stPersonInfo.szFacePicInfo[0].dwFileLenth = (uint)picture.Length;
                            stuInParam.stPersonInfo.szFacePicInfo[0].dwOffSet = 0;
                            stuInParam.nBufferLen = picture.Length;

                            pictureBufferPtr = Marshal.AllocHGlobal(stuInParam.nBufferLen);
                            Marshal.Copy(picture, 0, pictureBufferPtr, stuInParam.nBufferLen);
                            stuInParam.pBuffer = pictureBufferPtr;
                        }
                        else
                        {
                            _logger.LogWarning("No picture provided for user {UserName}", model.Name);
                            // Continue without picture - some devices might allow this
                        }

                        NET_OUT_OPERATE_FACERECONGNITIONDB stuOutParam = new NET_OUT_OPERATE_FACERECONGNITIONDB();
                        stuOutParam.dwSize = (uint)Marshal.SizeOf(typeof(NET_OUT_OPERATE_FACERECONGNITIONDB));

                        ret = NETClient.OperateFaceRecognitionDB(LoginID, ref stuInParam, ref stuOutParam, 5000); // 5000ms timeout

                        if (!ret)
                        {
                            string errorCode = NETClient.GetLastError();
                            _logger.LogError("Failed to add user {UserName} to device face database. Error: {ErrorCode}",
                                model.Name, errorCode);
                        }
                        else
                        {
                            _logger.LogInformation("Successfully added user {UserName} to device face database", model.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception occurred while adding user {UserName} to device face database", model.Name);
                        ret = false;
                    }
                    finally
                    {
                        // Clean up allocated memory
                        if (groupIdPtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(groupIdPtr);
                        if (groupNamePtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(groupNamePtr);
                        if (pictureBufferPtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(pictureBufferPtr);
                    }

                    return ret;
                });
            }
            finally
            {
                _deviceOperationSemaphore.Release();
            }
        }
        // You'll need to add this method to get group information
        public NET_FACERECONGNITION_GROUP_INFO? GetDefaultGroupInfo()
        {
            try
            {
                return FindGroups().FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get default group information");
                return null;
            }
        }

        private NET_FACERECONGNITION_GROUP_INFO[] FindGroups()
        {
            int nMax = 20;
            bool bRet = false;
            NET_IN_FIND_GROUP_INFO stuIn = new NET_IN_FIND_GROUP_INFO();
            NET_OUT_FIND_GROUP_INFO stuOut = new NET_OUT_FIND_GROUP_INFO();
            stuIn.dwSize = (uint)Marshal.SizeOf(stuIn);
            stuOut.dwSize = (uint)Marshal.SizeOf(stuOut);
            stuOut.nMaxGroupNum = nMax;
            try
            {
                stuOut.pGroupInfos = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NET_FACERECONGNITION_GROUP_INFO)) * nMax);
                NET_FACERECONGNITION_GROUP_INFO stuGroup = new NET_FACERECONGNITION_GROUP_INFO();
                stuGroup.dwSize = (uint)Marshal.SizeOf(stuGroup);
                for (int i = 0; i < nMax; i++)
                {
                    IntPtr pAdd = IntPtr.Add(stuOut.pGroupInfos, (int)stuGroup.dwSize * i);
                    Marshal.StructureToPtr(stuGroup, pAdd, true);
                }

                bRet = NETClient.FindGroupInfo(m_LoginID, ref stuIn, ref stuOut, m_WaitTime);
                if (bRet)
                {
                    NET_FACERECONGNITION_GROUP_INFO[] stuGroups = new NET_FACERECONGNITION_GROUP_INFO[stuOut.nRetGroupNum];
                    for (int i = 0; i < stuOut.nRetGroupNum; i++)
                    {
                        IntPtr pAdd = IntPtr.Add(stuOut.pGroupInfos, (int)Marshal.SizeOf(typeof(NET_FACERECONGNITION_GROUP_INFO)) * i);
                        stuGroups[i] = (NET_FACERECONGNITION_GROUP_INFO)Marshal.PtrToStructure(pAdd, typeof(NET_FACERECONGNITION_GROUP_INFO));
                    }
                    return stuGroups;
                }
                else
                {
                    _logger.LogError(NETClient.GetLastError());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            finally
            {
                Marshal.FreeHGlobal(stuOut.pGroupInfos);
            }
            return null;
        }
    }
}
