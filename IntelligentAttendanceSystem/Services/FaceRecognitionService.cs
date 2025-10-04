using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Hub;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetSDKCS;
using System.Runtime.InteropServices;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Services
{
    public class FaceRecognitionService : IFaceRecognitionService, IDisposable
    {
        private readonly IDahuaDeviceService _deviceService;
        private readonly ILogger<FaceRecognitionService> _logger;
        private readonly IHubContext<FaceRecognitionHub> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private IntPtr m_AnalyzerID = IntPtr.Zero;
        private fAnalyzerDataCallBack m_AnalyzerDataCallBack;
        private bool _isInitialized = false;
        private int _currentChannel = 0;
        private int _eventCounter = 0;
        private readonly List<FaceRecognitionEvent> _recentEvents = new List<FaceRecognitionEvent>();
        private readonly int _maxRecentEvents = 50;
        private Timer _healthCheckTimer;

        public bool IsFaceRecognationStart { get; private set; }

        public event Action<FaceRecognitionEvent> OnFaceRecognitionEvent;
        public event Action<FaceRecognitionEvent> OnFaceDetectionEvent;

        public FaceRecognitionService(
         IDahuaDeviceService deviceService,
         ILogger<FaceRecognitionService> logger,
         IHubContext<FaceRecognitionHub> hubContext,
         IServiceScopeFactory serviceScopeFactory)
        {
            _deviceService = deviceService;
            _logger = logger;
            _hubContext = hubContext;
            _serviceScopeFactory = serviceScopeFactory;

            InitializeCallbacks();
            StartHealthChecks();
        }
        private void StartHealthChecks()
        {
            _healthCheckTimer = new Timer(async _ =>
            {
                await SendHealthCheckAsync();
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private async Task SendHealthCheckAsync()
        {
            try
            {
                var healthInfo = new
                {
                    Timestamp = DateTime.Now,
                    IsDeviceConnected = _deviceService.IsDeviceConnected,
                    IsRecognitionRunning = m_AnalyzerID != IntPtr.Zero,
                    TotalEventsProcessed = _eventCounter,
                    RecentEventsCount = _recentEvents.Count,
                    ConnectedClients = FaceRecognitionHub.GetConnectedClientsCount()
                };

                await _hubContext.Clients.Group("FaceRecognition").SendAsync("HealthCheck", healthInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending health check");
            }
        }
        private void InitializeCallbacks()
        {
            if (!_isInitialized)
            {
                try
                {
                    m_AnalyzerDataCallBack = new fAnalyzerDataCallBack(AnalyzerDataCallBack);
                    _isInitialized = true;
                    _logger.LogInformation("Face recognition callbacks initialized");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize face recognition callbacks");
                    throw;
                }
            }
        }

        public async Task<bool> StartFaceRecognitionAsync(int channel = 0)
        {
            if (!_deviceService.IsDeviceConnected)
            {
                _logger.LogWarning("Cannot start face recognition - device not connected");
                return false;
            }

            if (m_AnalyzerID != IntPtr.Zero)
            {
                _logger.LogWarning("Face recognition is already running");
                return true;
            }

            try
            {
                _currentChannel = channel;

                m_AnalyzerID = NETClient.RealLoadPicture(
                    _deviceService.LoginID,
                    channel,
                    (uint)EM_EVENT_IVS_TYPE.ALL,
                    true,
                    m_AnalyzerDataCallBack,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (m_AnalyzerID == IntPtr.Zero)
                {
                    string error = NETClient.GetLastError();
                    _logger.LogError($"Failed to start face recognition: {error}");
                    return false;
                }

                _logger.LogInformation($"Face recognition started on channel {channel}");
                IsFaceRecognationStart = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception starting face recognition");
                IsFaceRecognationStart = false;
                return false;
            }
        }

        public async Task<bool> StopFaceRecognitionAsync()
        {
            if (m_AnalyzerID == IntPtr.Zero)
            {
                return true;
            }

            try
            {
                bool ret = NETClient.StopLoadPic(m_AnalyzerID);
                if (!ret)
                {
                    string error = NETClient.GetLastError();
                    _logger.LogError($"Failed to stop face recognition: {error}");
                    return false;
                }

                m_AnalyzerID = IntPtr.Zero;
                _logger.LogInformation("Face recognition stopped");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception stopping face recognition");
                return false;
            }
        }

        public Task<FaceRecognitionStatus> GetStatusAsync()
        {
            var status = new FaceRecognitionStatus
            {
                IsRunning = m_AnalyzerID != IntPtr.Zero,
                AnalyzerID = m_AnalyzerID,
                Channel = _currentChannel,
                LastEventTime = DateTime.Now, // You might want to track this properly
                TotalEventsProcessed = 0 // You might want to track this
            };

            return Task.FromResult(status);
        }

        // The main callback method from SDK
        private int AnalyzerDataCallBack(IntPtr lAnalyzerHandle, uint dwEventType, IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize, IntPtr dwUser, int nSequence, IntPtr reserved)
        {
            if (m_AnalyzerID == lAnalyzerHandle)
            {
                try
                {
                    switch (dwEventType)
                    {
                        case (uint)EM_EVENT_IVS_TYPE.FACERECOGNITION:
                            ProcessFaceRecognitionEvent(pEventInfo, pBuffer, dwBufSize);
                            break;
                        case (uint)EM_EVENT_IVS_TYPE.FACEDETECT:
                            ProcessFaceDetectionEvent(pEventInfo, pBuffer, dwBufSize);
                            break;
                        case (uint)EM_EVENT_IVS_TYPE.TRAFFICJUNCTION:
                            ProcessTrafficJunctionEvent(pEventInfo, pBuffer, dwBufSize);
                            break;
                        default:
                            _logger.LogDebug($"Unhandled event type: {dwEventType}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing analyzer callback");
                }
            }
            return 0;
        }
        private async void ProcessFaceRecognitionEvent(IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize)
        {
            try
            {
                NET_DEV_EVENT_FACERECOGNITION_INFO info = (NET_DEV_EVENT_FACERECOGNITION_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_FACERECOGNITION_INFO));

                var faceEvent = new FaceRecognitionEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "FACERECOGNITION",
                    EventTime = DateTime.Now,
                    FaceAttributes = ExtractFaceAttributes(info.stuFaceData)
                };

                // Extract images with size optimization
                if (IntPtr.Zero != pBuffer && dwBufSize > 0)
                {
                    // Global scene image (reduced quality for web)
                    if (info.bGlobalScenePic && info.stuGlobalScenePicInfo.dwFileLenth > 0)
                    {
                        faceEvent.GlobalImageBase64 = ExtractAndOptimizeImage(pBuffer, info.stuGlobalScenePicInfo.dwOffSet, info.stuGlobalScenePicInfo.dwFileLenth);
                    }

                    // Face image
                    if (info.stuObject.stPicInfo.dwFileLenth > 0)
                    {
                        faceEvent.FaceImageBase64 = ExtractAndOptimizeImage(pBuffer, info.stuObject.stPicInfo.dwOffSet, info.stuObject.stPicInfo.dwFileLenth, 150, 150);
                    }

                    // Candidate image
                    if (info.nCandidateNum > 0)
                    {
                        var candidatesInfo = info.stuCandidates.ToList().OrderByDescending(p => p.bySimilarity).ToArray();
                        NET_CANDIDATE_INFO maxSimilarityPersonInfo = candidatesInfo[0];

                        if (maxSimilarityPersonInfo.stPersonInfo.szFacePicInfo[0].dwFileLenth > 0)
                        {
                            faceEvent.CandidateImageBase64 = ExtractAndOptimizeImage(
                                pBuffer,
                                maxSimilarityPersonInfo.stPersonInfo.szFacePicInfo[0].dwOffSet,
                                maxSimilarityPersonInfo.stPersonInfo.szFacePicInfo[0].dwFileLenth,
                                150, 150);

                            faceEvent.CandidateInfo = ExtractCandidateInfo(maxSimilarityPersonInfo);
                            faceEvent.Similarity = maxSimilarityPersonInfo.bySimilarity;
                        }
                    }
                }

                _eventCounter++;
                faceEvent.EventNumber = _eventCounter;

                // Store in recent events (for new connections)
                _recentEvents.Add(faceEvent);
                if (_recentEvents.Count > _maxRecentEvents)
                {
                    _recentEvents.RemoveAt(0);
                }

                // Send via SignalR with error handling
                await SendSignalRMessageSafe("FaceRecognitionEvent", faceEvent);

                // Log attendance if similarity is high enough
                if (faceEvent.Similarity > 80 && faceEvent.CandidateInfo != null)
                {
                    _ = Task.Run(() => LogAttendanceAsync(faceEvent));
                }

                _logger.LogInformation($"Face recognition event #{_eventCounter}: {faceEvent.CandidateInfo?.Name ?? "Unknown"} (Similarity: {faceEvent.Similarity}%)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing face recognition event");
            }
        }
        private async Task SendSignalRMessageSafe(string method, object message)
        {
            try
            {
                await _hubContext.Clients.Group("FaceRecognition").SendAsync(method, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to send SignalR message {method}: {ex.Message}");
            }
        }
        private string ExtractAndOptimizeImage(IntPtr pBuffer, uint offset, uint length, int maxWidth = 400, int maxHeight = 300)
        {
            try
            {
                byte[] imageData = new byte[length];
                Marshal.Copy(IntPtr.Add(pBuffer, (int)offset), imageData, 0, (int)length);

                // For now, return the original image
                // In production, you might want to resize/optimize the image here
                return Convert.ToBase64String(imageData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting and optimizing image");
                return null;
            }
        }
        public List<FaceRecognitionEvent> GetRecentEvents(int count = 10)
        {
            return _recentEvents.TakeLast(count).ToList();
        }

        private async void ProcessFaceDetectionEvent(IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize)
        {
            try
            {
                NET_DEV_EVENT_FACEDETECT_INFO info = (NET_DEV_EVENT_FACEDETECT_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_FACEDETECT_INFO));

                var faceEvent = new FaceRecognitionEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "FACEDETECT",
                    EventTime = DateTime.Now,
                    FaceAttributes = new FaceAttributes
                    {
                        Sex = ParseSex(info.emSex),
                        Age = info.nAge,
                        SkinColor = ParseRace(info.emRace),
                        EyeState = ParseEye(info.emEye),
                        MouthState = ParseMouth(info.emMouth),
                        MaskState = ParseMask(info.emMask),
                        BeardState = ParseBeard(info.emBeard),
                        FaceQuality = info.nFaceQuality
                    }
                };

                // Extract global scene image
                if (pBuffer != IntPtr.Zero && dwBufSize > 0)
                {
                    faceEvent.GlobalImageBase64 = ExtractImageFromBuffer(pBuffer, 0, dwBufSize);
                }

                _eventCounter++;
                faceEvent.EventNumber = _eventCounter;

                // Send via SignalR
                await _hubContext.Clients.Group("FaceRecognition").SendAsync("FaceDetectionEvent", faceEvent);

                _logger.LogInformation($"Face detection event #{_eventCounter}: {faceEvent.FaceAttributes.Sex}, {faceEvent.FaceAttributes.Age} years");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing face detection event");
            }
        }

        private async Task LogAttendanceAsync(FaceRecognitionEvent faceEvent)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get user details if available
                FaceUser user = null;
                if (!string.IsNullOrEmpty(faceEvent.CandidateInfo?.Id))
                {
                    user = await context.FaceUsers
                        .FirstOrDefaultAsync(u => u.CredentialNumber == faceEvent.CandidateInfo.Id);
                }

                var attendance = new FaceAttendanceRecord
                {
                    EventId = faceEvent.EventId,
                    UserId = faceEvent.CandidateInfo?.Id,
                    UserName = faceEvent.CandidateInfo?.Name,
                    Similarity = faceEvent.Similarity,
                    EventTime = faceEvent.EventTime,
                    FaceImageBase64 = faceEvent.FaceImageBase64,
                    CandidateImageBase64 = faceEvent.CandidateImageBase64,
                    GlobalImageBase64 = faceEvent.GlobalImageBase64,
                    EventType = "RECOGNITION",
                    CreatedDate = DateTime.UtcNow,
                    // Set additional properties from user if available
                    Department = (user != null ? user.Department : ""),
                    Position = (user != null ? user.Position : ""),
                    Gender = (user != null ? user.Gender : 0)
                };

                context.FaceAttendanceRecords.Add(attendance);
                await context.SaveChangesAsync();

                // Notify clients about new attendance
                await _hubContext.Clients.Group("FaceRecognition").SendAsync("AttendanceLogged", new
                {
                    attendance.UserName,
                    attendance.UserId,
                    attendance.Similarity,
                    attendance.EventTime,
                    Department = user?.Department
                });
                await context.Database.ExecuteSqlRawAsync("EXEC SyncAllAttendances;");
                _logger.LogInformation($"Attendance logged for: {faceEvent.CandidateInfo?.Name} (Similarity: {faceEvent.Similarity}%)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging attendance for face recognition event");
            }
        }

        private string ExtractImageFromBuffer(IntPtr pBuffer, uint offset, uint length)
        {
            try
            {
                byte[] imageData = new byte[length];
                Marshal.Copy(IntPtr.Add(pBuffer, (int)offset), imageData, 0, (int)length);
                return Convert.ToBase64String(imageData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting image from buffer");
                return null;
            }
        }

        private FaceAttributes ExtractFaceAttributes(NET_FACE_DATA faceData)
        {
            return new FaceAttributes
            {
                Sex = ParseSex(faceData.emSex),
                Age = faceData.nAge,
                SkinColor = ParseRace(faceData.emRace),
                EyeState = ParseEye(faceData.emEye),
                MouthState = ParseMouth(faceData.emMouth),
                MaskState = ParseMask(faceData.emMask),
                BeardState = ParseBeard(faceData.emBeard),
                FaceQuality = faceData.nFaceQuality
            };
        }

        private CandidateInfo ExtractCandidateInfo(NET_CANDIDATE_INFO candidateInfo)
        {
            return new CandidateInfo
            {
                Name = candidateInfo.stPersonInfo.szPersonNameEx,
                Id = candidateInfo.stPersonInfo.szID,
                Sex = candidateInfo.stPersonInfo.bySex == 1 ? "Male" :
                      candidateInfo.stPersonInfo.bySex == 2 ? "Female" : "Unknown",
                Birthday = $"{candidateInfo.stPersonInfo.wYear:D4}-{candidateInfo.stPersonInfo.byMonth:D2}-{candidateInfo.stPersonInfo.byDay:D2}",
                GroupId = Marshal.PtrToStringAnsi(candidateInfo.stPersonInfo.pszGroupID),
                GroupName = Marshal.PtrToStringAnsi(candidateInfo.stPersonInfo.pszGroupName)
            };
        }

        // Helper methods for parsing enums to strings
        private string ParseSex(EM_DEV_EVENT_FACEDETECT_SEX_TYPE sex)
        {
            return sex switch
            {
                EM_DEV_EVENT_FACEDETECT_SEX_TYPE.MAN => "Male",
                EM_DEV_EVENT_FACEDETECT_SEX_TYPE.WOMAN => "Female",
                _ => "Unknown"
            };
        }

        private string ParseRace(EM_RACE_TYPE race)
        {
            return race.ToString().Replace("_", " ");
        }

        private string ParseEye(EM_EYE_STATE_TYPE eye)
        {
            return eye.ToString().Replace("_", " ");
        }

        private string ParseMouth(EM_MOUTH_STATE_TYPE mouth)
        {
            return mouth.ToString().Replace("_", " ");
        }

        private string ParseMask(EM_MASK_STATE_TYPE mask)
        {
            return mask.ToString().Replace("_", " ");
        }

        private string ParseBeard(EM_BEARD_STATE_TYPE beard)
        {
            return beard.ToString().Replace("_", " ");
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            StopFaceRecognitionAsync().Wait();
        }
    }
}
