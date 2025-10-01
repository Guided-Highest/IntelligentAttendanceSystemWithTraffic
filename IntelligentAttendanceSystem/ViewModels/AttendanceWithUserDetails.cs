using IntelligentAttendanceSystem.Models;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class AttendanceWithUserDetails
    {
        public FaceAttendanceRecord AttendanceRecord { get; set; }
        public FaceUser User { get; set; }
    }
}
