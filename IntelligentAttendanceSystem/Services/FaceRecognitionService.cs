using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Hub;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.Structures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetSDKCS;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Services
{
    public class FaceRecognitionService : IFaceRecognitionService, IDisposable
    {
        private readonly ConcurrentDictionary<int, ChannelRecognitionState> _channelStates;
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
            _serviceScopeFactory = serviceScopeFactory;// Add this
            _channelStates = new ConcurrentDictionary<int, ChannelRecognitionState>();
            using var scope = _serviceScopeFactory.CreateScope();
            var _vehicleCountingService = scope.ServiceProvider.GetRequiredService<IVehicleCountingService>();
            // Subscribe to vehicle detection events
            _vehicleCountingService.OnVehicleDetected += OnVehicleDetected;
            InitializeCallbacks();
            StartHealthChecks();
        }
        private void OnVehicleDetected(object sender, VehicleDetectionEvent e)
        {
            // Handle vehicle detection events if needed
            _logger.LogInformation($"Vehicle detected: {e.VehicleType} moving {e.Direction}");
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendVehicleDetection(e);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending vehicle detection via SignalR");
                }
            });
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
            if (_channelStates.ContainsKey(channel) && _channelStates[channel].IsRunning)
            {
                _logger.LogWarning($"Face recognition is already running on channel {channel}");
                return true;
            }

            try
            {
                //_currentChannel = channel;

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

                var state = new ChannelRecognitionState
                {
                    AnalyzerID = m_AnalyzerID,
                    IsRunning = true,
                    Channel = channel
                };
                _channelStates[channel] = state;


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

        public async Task<bool> StopFaceRecognitionAsync(int channel = 0)
        {

            if (_channelStates.TryGetValue(channel, out var state))
            {
                try
                {
                    // Stop the analyzer for this channel
                    if (state.AnalyzerID != IntPtr.Zero)
                    {
                        bool ret = NETClient.StopLoadPic(state.AnalyzerID);
                        if (!ret)
                        {
                            string error = NETClient.GetLastError();
                            _logger.LogError($"Failed to stop face recognition: {error}");
                            return false;
                        }
                    }

                    _channelStates.TryRemove(channel, out _);
                    _logger.LogInformation($"Face recognition stopped on channel {channel}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception stopping face recognition on channel {channel}");
                    return false;
                }
            }

            _logger.LogWarning($"Face recognition is not running on channel {channel}");
            return false;
        }
        public bool IsChannelRunning(int channel)
        {
            return _channelStates.ContainsKey(channel) && _channelStates[channel].IsRunning;
        }

        public List<int> GetRunningChannels()
        {
            return _channelStates.Where(x => x.Value.IsRunning).Select(x => x.Key).ToList();
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
            var channelState = _channelStates.Values.FirstOrDefault(state => state.AnalyzerID == lAnalyzerHandle);
            if (channelState != null)
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
                            ProcessTrafficJunctionEvent(channelState.Channel, pEventInfo, pBuffer, dwBufSize);
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
            else
            {
                _logger.LogWarning($"Received callback for unknown analyzer handle: {lAnalyzerHandle}");
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
                await SendSignalRMessageSafe("FaceRecognition", "FaceRecognitionEvent", faceEvent);

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
        private async Task SendSignalRMessageSafe(string group, string method, object message)
        {
            try
            {
                await _hubContext.Clients.Group(group).SendAsync(method, message);
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
            if (pBuffer == IntPtr.Zero || length == 0 || offset >= uint.MaxValue - length)
            {
                _logger.LogWarning($"Invalid buffer parameters - Offset: {offset}, Length: {length}, Buffer: {pBuffer}");
                return null;
            }
            try
            {
                // Validate that the offset + length doesn't exceed reasonable bounds
                if (length > 10 * 1024 * 1024) // 10MB max image size
                {
                    _logger.LogWarning($"Image size too large: {length} bytes");
                    return null;
                }
                byte[] imageData = new byte[length];
                // Calculate the source pointer
                IntPtr sourcePtr = IntPtr.Add(pBuffer, (int)offset);

                // Verify the pointer is valid (basic check)
                if (sourcePtr == IntPtr.Zero)
                {
                    _logger.LogWarning("Invalid source pointer after offset calculation");
                    return null;
                }
                Marshal.Copy(IntPtr.Add(pBuffer, (int)offset), imageData, 0, (int)length);

                // Validate that it's actually image data
                if (IsValidImageData(imageData))
                {
                    // Convert to base64
                    string base64String = Convert.ToBase64String(imageData);

                    _logger.LogDebug($"Successfully extracted image: {length} bytes");
                    return base64String;
                }
                else
                {
                    _logger.LogWarning("Extracted data doesn't appear to be valid image data");
                    return null;
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogError(ex, $"Argument out of range in ExtractImageFromBuffer: offset={offset}, length={length}");
                return null;
            }
            catch (AccessViolationException ex)
            {
                _logger.LogError(ex, $"Access violation in ExtractImageFromBuffer: offset={offset}, length={length}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error in ExtractImageFromBuffer: offset={offset}, length={length}");
                return null;
            }
        }

        private bool IsValidImageData(byte[] data)
        {
            if (data == null || data.Length < 8)
                return false;

            // Check for common image file signatures
            // JPEG: FF D8 FF
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return true;

            // BMP: 42 4D
            if (data[0] == 0x42 && data[1] == 0x4D)
                return true;

            return false;
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
        #region Trafic Count
        private async void ProcessTrafficJunctionEvent(int channel, IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize)
        {
            try
            {
                // Parse the event information structure
                NET_DEV_EVENT_TRAFFICJUNCTION_INFO info = (NET_DEV_EVENT_TRAFFICJUNCTION_INFO)Marshal.PtrToStructure(
                    pEventInfo, typeof(NET_DEV_EVENT_TRAFFICJUNCTION_INFO));

                // Extract vehicle information from stuObject
                var vehicleType = GetVehicleTypeFromObject(info.stuObject);
                var direction = ParseDirection(info.byDirection);
                var plateNumber = GetPlateNumber(info);
                var speed = info.nSpeed;
                var junctionId = info.nChannelID;
                var confidence = info.stuObject.nConfidence;
                var vehicleSize = CalculateVehicleSize(info.stuObject.BoundingBox);

                using var scope = _serviceScopeFactory.CreateScope();
                var _vehicleCountingService = scope.ServiceProvider.GetRequiredService<IVehicleCountingService>();
                // Count vehicles based on type and direction
                await _vehicleCountingService.CountVehicleAsync(
                    junctionId,
                    vehicleType,
                    direction,
                    vehicleSize,
                    plateNumber,
                    speed,
                    confidence);

                // Create event info for notification
                var eventInfo = new VehicleDetectionEvent
                {
                    EventId = $"{info.nChannelID}_{info.nEventID}_{info.UTC.ToDateTime():yyyyMMddHHmmss}",
                    EventTime = info.UTC.ToDateTime(),
                    JunctionId = junctionId,
                    VehicleType = vehicleType,
                    Direction = direction,
                    VehicleSize = vehicleSize,
                    PlateNumber = plateNumber,
                    Speed = speed,
                    Confidence = confidence,
                    ObjectId = info.stuObject.nObjectID,
                    BoundingBox = ExtractBoundingBox(info.stuObject.BoundingBox),
                    SourceChannel = channel // Add channel info
                };


                // Process the traffic event (your existing code)
                var trafficEvent = new TrafficJunctionEvent
                {
                    EventId = eventInfo.EventId,
                    EventTime = eventInfo.EventTime,
                    VehicleInfo = ExtractVehicleInfo(info),
                    EventNumber = Interlocked.Increment(ref _eventCounter),
                    ChannelId = junctionId,
                    SourceChannel = channel, // Track which channel this came from
                    EventAction = info.bEventAction == 1 ? "Start" : "Stop"
                };
                // Extract images using available fields
                if (IntPtr.Zero != pBuffer && dwBufSize > 0)
                {
                    // Extract scene image if available
                    if (info.bSceneImage && info.stuSceneImage.nLength > 0 && info.stuSceneImage.nLength <= dwBufSize)
                    {
                        trafficEvent.GlobalImageBase64 = ExtractImageFromBuffer(
                            pBuffer, info.stuSceneImage.nOffSet, info.stuSceneImage.nLength);
                    }

                    // Extract object image if available (from stuObject)
                    if (info.stuObject.bPicEnble == 1 && info.stuObject.stPicInfo.dwFileLenth > 0 && info.stuObject.stPicInfo.dwFileLenth <= dwBufSize)
                    {
                        trafficEvent.VehicleInfo.VehicleImageBase64 = ExtractImageFromBuffer(
                            pBuffer, info.stuObject.stPicInfo.dwOffSet, info.stuObject.stPicInfo.dwFileLenth);
                    }

                    // Try to extract plate image if available
                    // Note: You might need to check if plate image info is available in your SDK
                    trafficEvent.VehicleInfo.PlateImageBase64 = ExtractPlateImage(info, pBuffer, dwBufSize);
                }

                // Send via SignalR
                await SendSignalRMessageSafe("TrafficMonitoring", "TrafficJunctionEvent", trafficEvent);

                await SendVehicleDetection(eventInfo);
                // Also send vehicle count update
                await SendVehicleCountUpdate(channel, junctionId);

                // Log to database
                _ = Task.Run(() => LogTrafficEventAsync(trafficEvent));

                _logger.LogInformation($"Traffic junction event processed: {vehicleType} (Confidence: {confidence}%) moving {direction} at junction {junctionId}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing traffic junction event");
            }
        }
        private async Task LogTrafficEventAsync(TrafficJunctionEvent trafficEvent)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var trafficRecord = new TrafficRecord()
                {
                    EventId = trafficEvent.EventId,
                    VehicleType = trafficEvent.VehicleInfo.VehicleType,
                    PlateNumber = trafficEvent.VehicleInfo.PlateNumber,
                    Color = trafficEvent.VehicleInfo.Color,
                    Speed = trafficEvent.VehicleInfo.Speed,
                    ViolationType = "",// trafficEvent.ViolationInfo != null ? trafficEvent.ViolationInfo.ViolationType : "",
                    ViolationDescription = "",// trafficEvent.ViolationInfo != null ? trafficEvent.ViolationInfo.Description : "",
                    Confidence = 0,// trafficEvent.ViolationInfo?.Confidence,
                    JunctionId = trafficEvent.JunctionInfo != null ? trafficEvent.JunctionInfo.JunctionId : "",
                    LaneNumber = "",//trafficEvent.ViolationInfo != null ? trafficEvent.ViolationInfo.LaneNumber : "",
                    EventTime = trafficEvent.EventTime,
                    GlobalImageBase64 = trafficEvent.GlobalImageBase64 != null ? trafficEvent.GlobalImageBase64 : "",
                    VehicleImageBase64 = trafficEvent.VehicleInfo != null ? trafficEvent.VehicleInfo.VehicleImageBase64 != null ?
                    trafficEvent.VehicleInfo.VehicleImageBase64 : "" : "",
                    PlateImageBase64 = trafficEvent.VehicleInfo != null ? trafficEvent.VehicleInfo.PlateImageBase64 != null ?
                    trafficEvent.VehicleInfo.PlateImageBase64 : "" : "",
                    CreatedDate = DateTime.UtcNow
                };

                context.trafficRecords.Add(trafficRecord);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Traffic violation logged: {trafficEvent.VehicleInfo.PlateNumber} - {(trafficEvent.ViolationInfo == null ? "" : trafficEvent.ViolationInfo.ViolationType)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging traffic event to database");
            }
        }
        #region Vehicle Counting Logic
        private async Task SendVehicleCountUpdate(int channel, int junctionId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var _vehicleCountingService = scope.ServiceProvider.GetRequiredService<IVehicleCountingService>();
                var today = DateTime.Today;
                var stats = await _vehicleCountingService.GetCountingStatsAsync(today, DateTime.Now, junctionId);

                var countUpdate = new
                {
                    Channel = channel,
                    JunctionId = junctionId,
                    TotalToday = stats.TotalVehicles,
                    VehicleTypeCounts = stats.VehicleTypeCounts,
                    DirectionCounts = stats.DirectionCounts,
                    TypeDirectionMatrix = stats.TypeDirectionMatrix,
                    LastUpdate = DateTime.Now,
                    HourlyStats = await GetCurrentHourStats(junctionId)
                };

                await _hubContext.Clients.Group("TrafficMonitoring").SendAsync("VehicleCountUpdate", countUpdate);

                _logger.LogDebug($"Sent VehicleCountUpdate for junction {junctionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vehicle count update");
            }
        }
        // Add this method to send individual vehicle detections
        private async Task SendVehicleDetection(VehicleDetectionEvent vehicleEvent)
        {
            try
            {
                var detectionData = new
                {
                    vehicleEvent.EventId,
                    vehicleEvent.EventTime,
                    vehicleEvent.JunctionId,
                    vehicleEvent.VehicleType,
                    vehicleEvent.Direction,
                    vehicleEvent.VehicleSize,
                    vehicleEvent.PlateNumber,
                    vehicleEvent.Speed,
                    vehicleEvent.Confidence,
                    vehicleEvent.ConfidenceLevel,
                    vehicleEvent.ObjectId,
                    vehicleEvent.BoundingBox,
                    Timestamp = DateTime.Now
                };

                await _hubContext.Clients.Group("TrafficMonitoring").SendAsync("VehicleDetected", vehicleEvent);

                _logger.LogDebug($"Sent VehicleDetected: {vehicleEvent.VehicleType} moving {vehicleEvent.Direction}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vehicle detection");
            }
        }
        // In FaceRecognitionService - Update the OnVehicleCountingDetected method
        private void OnVehicleCountingDetected(object sender, VehicleDetectionEvent e)
        {
            // Handle vehicle detection events
            _logger.LogInformation($"Vehicle counted: {e.VehicleType} moving {e.Direction} at junction {e.JunctionId}");

            // Send real-time update via SignalR
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendVehicleDetection(e);

                    // Also update the counts
                    await SendVehicleCountUpdate(e.SourceChannel, e.JunctionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending vehicle detection via SignalR");
                }
            });
        }
        private async Task<object> GetCurrentHourStats(int junctionId)
        {
            try
            {
                var currentHour = DateTime.Now.Hour;
                var today = DateTime.Today;
                var hourStart = today.AddHours(currentHour);
                var hourEnd = hourStart.AddHours(1).AddSeconds(-1);

                using var scope = _serviceScopeFactory.CreateScope();
                var _vehicleCountingService = scope.ServiceProvider.GetRequiredService<IVehicleCountingService>();
                var stats = await _vehicleCountingService.GetCountingStatsAsync(hourStart, hourEnd, junctionId);

                return new
                {
                    Hour = currentHour,
                    Total = stats.TotalVehicles,
                    ByType = stats.VehicleTypeCounts,
                    ByDirection = stats.DirectionCounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current hour stats");
                return new { Hour = DateTime.Now.Hour, Total = 0, ByType = new object(), ByDirection = new object() };
            }
        }
        #endregion
        #region TC Image Process
        private void ProcessVehicleImage(IntPtr pBuffer, uint dwBufSize, NET_DEV_EVENT_TRAFFICJUNCTION_INFO eventInfo)
        {
            try
            {
                // Convert the image buffer to a byte array
                byte[] imageData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, imageData, 0, (int)dwBufSize);

                // Save image or process further
                string fileName = $"Vehicle_{eventInfo.stTrafficCar.nVehicleSize}_{DateTime.Now:yyyyMMddHHmmssfff}.jpg";
                File.WriteAllBytes(fileName, imageData);

                _logger.LogInformation($"Vehicle image saved: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vehicle image");
            }
        }
        private string ExtractPlateImage(NET_DEV_EVENT_TRAFFICJUNCTION_INFO info, IntPtr pBuffer, uint dwBufSize)
        {
            try
            {
                // Method 1: Check if there's a dedicated plate image in the structure
                // This depends on your specific SDK version and configuration

                // Method 2: Use the object image as plate image if it contains the plate
                if (info.stuObject.bPicEnble == 1 && info.stuObject.stPicInfo.dwFileLenth > 0)
                {
                    // You might want to create a cropped version showing just the plate
                    // For now, return the object image as it likely contains the vehicle with plate
                    return ExtractImageFromBuffer(
                        pBuffer, info.stuObject.stPicInfo.dwOffSet, info.stuObject.stPicInfo.dwFileLenth);
                }

                // Method 3: Check if there are additional objects that might contain plate images
                if (info.pstObjects != IntPtr.Zero && info.nObjectNum > 0)
                {
                    // There might be multiple objects, one of which could be the license plate
                    return ExtractPlateFromMultipleObjects(info, pBuffer, dwBufSize);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting plate image");
                return null;
            }
        }
        private string ExtractPlateFromMultipleObjects(NET_DEV_EVENT_TRAFFICJUNCTION_INFO info, IntPtr pBuffer, uint dwBufSize)
        {
            try
            {
                int objectSize = Marshal.SizeOf(typeof(NET_MSG_OBJECT));

                for (int i = 0; i < info.nObjectNum; i++)
                {
                    IntPtr objectPtr = IntPtr.Add(info.pstObjects, i * objectSize);
                    NET_MSG_OBJECT obj = (NET_MSG_OBJECT)Marshal.PtrToStructure(objectPtr, typeof(NET_MSG_OBJECT));

                    // Check if this object might be a license plate
                    string objectType = GetStringFromByteArray(obj.szObjectType);
                    if (objectType.Contains("plate", StringComparison.OrdinalIgnoreCase) ||
                        objectType.Contains("license", StringComparison.OrdinalIgnoreCase))
                    {
                        if (obj.bPicEnble == 1 && obj.stPicInfo.dwFileLenth > 0)
                        {
                            return ExtractImageFromBuffer(pBuffer, obj.stPicInfo.dwOffSet, obj.stPicInfo.dwFileLenth);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting plate from multiple objects");
            }

            return null;
        }
        #endregion
        #region Extraction Method

        private string GetPlateNumber(NET_DEV_EVENT_TRAFFICJUNCTION_INFO info)
        {
            // Get plate number from plate info
            if (!string.IsNullOrEmpty(info.stuPlateInfo.szFrontPlateNumber))
            {
                string plate = info.stuPlateInfo.szFrontPlateNumber;
                if (!string.IsNullOrEmpty(plate) && plate != "Unknown")
                    return plate;
            }
            // Get plate number from plate info
            if (!string.IsNullOrEmpty(info.stuPlateInfo.szBackPlateNumber))
            {
                string plate = info.stuPlateInfo.szFrontPlateNumber;
                if (!string.IsNullOrEmpty(plate) && plate != "Unknown")
                    return plate;
            }

            // Fallback to traffic car info
            if (!string.IsNullOrEmpty(info.stTrafficCar.szPlateNumber))
            {
                string plate = info.stTrafficCar.szPlateNumber;
                if (!string.IsNullOrEmpty(plate))
                    return plate;
            }

            return "Unknown";
        }
        // New helper methods for extracting information from NET_MSG_OBJECT
        private string GetVehicleTypeFromObject(NET_MSG_OBJECT stuObject)
        {
            // First try to get from object type string
            if (stuObject.szObjectType != null && stuObject.szObjectType.Length > 0)
            {
                string objectType = GetStringFromByteArray(stuObject.szObjectType);
                if (!string.IsNullOrEmpty(objectType) && objectType != "Unknown")
                {
                    return ParseObjectTypeFromString(objectType);
                }
            }

            // Try object sub-type
            if (stuObject.szObjectSubType != null && stuObject.szObjectSubType.Length > 0)
            {
                string subType = GetStringFromByteArray(stuObject.szObjectSubType);
                if (!string.IsNullOrEmpty(subType))
                {
                    return subType;
                }
            }

            // Fallback to text field
            if (stuObject.szText != null && stuObject.szText.Length > 0)
            {
                string text = GetStringFromByteArray(stuObject.szText);
                if (!string.IsNullOrEmpty(text))
                {
                    return ExtractVehicleTypeFromText(text);
                }
            }

            return "Unknown";
        }

        private string ParseObjectTypeFromString(string objectType)
        {
            if (objectType.Contains("vehicle", StringComparison.OrdinalIgnoreCase) ||
                objectType.Contains("car", StringComparison.OrdinalIgnoreCase))
                return "Car";

            if (objectType.Contains("truck", StringComparison.OrdinalIgnoreCase))
                return "Truck";

            if (objectType.Contains("bus", StringComparison.OrdinalIgnoreCase))
                return "Bus";

            if (objectType.Contains("motorcycle", StringComparison.OrdinalIgnoreCase) ||
                objectType.Contains("motorbike", StringComparison.OrdinalIgnoreCase))
                return "Motorcycle";

            if (objectType.Contains("bicycle", StringComparison.OrdinalIgnoreCase) ||
                objectType.Contains("bike", StringComparison.OrdinalIgnoreCase))
                return "Bicycle";

            if (objectType.Contains("person", StringComparison.OrdinalIgnoreCase) ||
                objectType.Contains("pedestrian", StringComparison.OrdinalIgnoreCase))
                return "Pedestrian";

            return objectType;
        }

        private string ExtractVehicleTypeFromText(string text)
        {
            // Common patterns in license plate recognition or object detection
            if (text.Contains("CAR") || text.Contains("car"))
                return "Car";

            if (text.Contains("TRUCK") || text.Contains("truck"))
                return "Truck";

            if (text.Contains("BUS") || text.Contains("bus"))
                return "Bus";

            if (text.Contains("MOTOR") || text.Contains("motor"))
                return "Motorcycle";

            if (text.Contains("BIKE") || text.Contains("bike"))
                return "Bicycle";

            return "Unknown";
        }

        private int CalculateVehicleSize(NET_RECT boundingBox)
        {
            // Calculate area of bounding box as a rough size indicator
            int width = boundingBox.nRight - boundingBox.nLeft;
            int height = boundingBox.nBottom - boundingBox.nTop;

            if (width <= 0 || height <= 0)
                return 0;

            return width * height;
        }

        private Rect ExtractBoundingBox(NET_RECT netRect)
        {
            return new Rect
            {
                X = netRect.nLeft,
                Y = netRect.nTop,
                Width = netRect.nRight - netRect.nLeft,
                Height = netRect.nBottom - netRect.nTop
            };
        }

        private string GetStringFromByteArray(byte[] byteArray)
        {
            try
            {
                if (byteArray == null || byteArray.Length == 0)
                    return string.Empty;

                // Find the first null terminator
                int length = Array.IndexOf(byteArray, (byte)0);
                if (length < 0) length = byteArray.Length;

                if (length > 0)
                {
                    return System.Text.Encoding.ASCII.GetString(byteArray, 0, length).Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting byte array to string");
            }

            return string.Empty;
        }

        // Extract additional object attributes
        private Dictionary<string, object> ExtractObjectAttributes(NET_MSG_OBJECT stuObject)
        {
            var attributes = new Dictionary<string, object>();

            try
            {
                attributes["ObjectID"] = stuObject.nObjectID;
                attributes["Confidence"] = stuObject.nConfidence;
                attributes["Action"] = stuObject.nAction;

                // Bounding box info
                attributes["BoundingBox"] = new
                {
                    Left = stuObject.BoundingBox.nLeft,
                    Top = stuObject.BoundingBox.nTop,
                    Right = stuObject.BoundingBox.nRight,
                    Bottom = stuObject.BoundingBox.nBottom,
                    Width = stuObject.BoundingBox.nRight - stuObject.BoundingBox.nLeft,
                    Height = stuObject.BoundingBox.nBottom - stuObject.BoundingBox.nTop
                };

                // Center point
                attributes["Center"] = new
                {
                    X = stuObject.Center.nx,
                    Y = stuObject.Center.ny
                };

                // Object type and text
                attributes["ObjectType"] = GetStringFromByteArray(stuObject.szObjectType);
                attributes["ObjectSubType"] = GetStringFromByteArray(stuObject.szObjectSubType);
                attributes["Text"] = GetStringFromByteArray(stuObject.szText);
                attributes["SubText"] = GetStringFromByteArray(stuObject.szSubText);

                // Color information
                attributes["MainColor"] = $"#{stuObject.rgbaMainColor:X8}";
                attributes["HasImage"] = stuObject.bPicEnble == 1;
                attributes["IsShotFrame"] = stuObject.bShotFrame == 1;
                attributes["HasColor"] = stuObject.bColor == 1;

                // Timing information
                attributes["CurrentTime"] = stuObject.stuCurrentTime.ToDateTime();
                attributes["StartTime"] = stuObject.stuStartTime.ToDateTime();
                attributes["EndTime"] = stuObject.stuEndTime.ToDateTime();

                // Sequence information
                attributes["CurrentSequence"] = stuObject.dwCurrentSequence;
                attributes["BeginSequence"] = stuObject.dwBeginSequence;
                attributes["EndSequence"] = stuObject.dwEndSequence;

                // Additional identifiers
                attributes["RelativeID"] = stuObject.nRelativeID;
                attributes["SubBrand"] = stuObject.wSubBrand;
                attributes["BrandYear"] = stuObject.wBrandYear;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting object attributes");
            }

            return attributes;
        }
        private string ParseDirection(byte byDirection)
        {
            return byDirection switch
            {
                0 => "Left",
                1 => "Right",
                2 => "Straight",
                3 => "Left Turn",
                4 => "Right Turn",
                5 => "U-Turn",
                _ => "Unknown"
            };
        }
        private string ParseVehiclePosture(EM_VEHICLE_POSTURE_TYPE posture)
        {
            return posture switch
            {
                EM_VEHICLE_POSTURE_TYPE.VEHICLE_HEAD => "Head",
                EM_VEHICLE_POSTURE_TYPE.VEHICLE_SIDE => "Side",
                EM_VEHICLE_POSTURE_TYPE.VEHICLE_TAIL => "Tail",
                _ => "Unknown"
            };
        }

        private TrafficVehicleInfo ExtractVehicleInfo(NET_DEV_EVENT_TRAFFICJUNCTION_INFO info)
        {
            var vehicleInfo = new TrafficVehicleInfo
            {
                PlateNumber = GetPlateNumber(info),
                VehicleType = GetVehicleTypeFromObject(info.stuObject), // Use the new method
                Color = GetVehicleColor(info),
                Speed = info.nSpeed,
                Direction = ParseDirection(info.byDirection),
                VehicleRect = ExtractBoundingBox(info.stuObject.BoundingBox),
                DriverSeatBelt = info.byMainSeatBelt == 1,
                PassengerSeatBelt = info.bySlaveSeatBelt == 1,
                VehiclePosture = ParseVehiclePosture(info.emVehiclePosture),
                VehicleSignConfidence = info.nVehicleSignConfidence,
                VehicleCategoryConfidence = info.nVehicleCategoryConfidence,
                ObjectConfidence = info.stuObject.nConfidence,
                ObjectAttributes = ExtractObjectAttributes(info.stuObject) // Add object attributes
            };

            return vehicleInfo;
        }
        private string GetVehicleColor(NET_DEV_EVENT_TRAFFICJUNCTION_INFO info)
        {
            // Try to get from traffic car info
            if (!string.IsNullOrEmpty(info.stTrafficCar.szVehicleColor))
            {
                return info.stTrafficCar.szVehicleColor;
            }

            return "Unknown";
        }
        #endregion
        #endregion
        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            // Stop all running channels when service is disposed
            foreach (var channel in _channelStates.Keys.ToList())
            {
                StopFaceRecognitionAsync(channel).Wait(5000); // Wait up to 5 seconds
            }
            _channelStates.Clear();
        }
    }
}
