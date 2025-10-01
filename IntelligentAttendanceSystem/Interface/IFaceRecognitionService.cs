using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IFaceRecognitionService
    {
        bool IsFaceRecognationStart { get; }
        Task<bool> StartFaceRecognitionAsync(int channel = 0);
        Task<bool> StopFaceRecognitionAsync();
        Task<FaceRecognitionStatus> GetStatusAsync();
        event Action<FaceRecognitionEvent> OnFaceRecognitionEvent;
        event Action<FaceRecognitionEvent> OnFaceDetectionEvent;
    }
}
