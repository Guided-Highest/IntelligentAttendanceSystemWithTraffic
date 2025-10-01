namespace IntelligentAttendanceSystem.Models
{
    public class FaceRecognitionModels
    {
        public class FaceRecognitionEvent
        {
            public string EventId { get; set; }
            public string EventType { get; set; } // "FACERECOGNITION" or "FACEDETECT"
            public string GlobalImageBase64 { get; set; }
            public string FaceImageBase64 { get; set; }
            public string CandidateImageBase64 { get; set; }
            public DateTime EventTime { get; set; }
            public FaceAttributes FaceAttributes { get; set; }
            public CandidateInfo CandidateInfo { get; set; }
            public float Similarity { get; set; }
            public int EventNumber { get; set; }
        }

        public class FaceAttributes
        {
            public string Sex { get; set; }
            public int Age { get; set; }
            public string SkinColor { get; set; }
            public string EyeState { get; set; }
            public string MouthState { get; set; }
            public string MaskState { get; set; }
            public string BeardState { get; set; }
            public uint FaceQuality { get; set; }
        }

        public class CandidateInfo
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public string Sex { get; set; }
            public string Birthday { get; set; }
            public string GroupId { get; set; }
            public string GroupName { get; set; }
        }

        public class FaceRecognitionStatus
        {
            public bool IsRunning { get; set; }
            public IntPtr AnalyzerID { get; set; }
            public int Channel { get; set; }
            public DateTime LastEventTime { get; set; }
            public int TotalEventsProcessed { get; set; }
        }

        public class FaceAttendanceRecord
        {
            public int Id { get; set; }
            public string EventId { get; set; }
            public string? UserId { get; set; }
            public string UserName { get; set; }
            public float Similarity { get; set; }
            public DateTime EventTime { get; set; }
            public string FaceImageBase64 { get; set; }
            public string CandidateImageBase64 { get; set; }
            public string GlobalImageBase64 { get; set; }
            public DateTime CreatedDate { get; set; }

            // Add these properties for better reporting
            public string Department { get; set; }
            public string Position { get; set; }
            public Gender Gender { get; set; }
            public string EventType { get; set; } // "RECOGNITION" or "DETECTION"
            public virtual FaceUser? User { get; set; }
        }
    }
}
