namespace IntelligentAttendanceSystem.Models
{
    // Models/ReportingModels.cs
    public class FaceRecognitionReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRecognitions { get; set; }
        public int TotalDetections { get; set; }
        public int UniqueUsers { get; set; }
        public double AverageSimilarity { get; set; }
        public List<RecognitionStats> HourlyStats { get; set; }
        public List<UserRecognitionCount> TopUsers { get; set; }
        public List<DepartmentStats> DepartmentStats { get; set; }
    }

    public class RecognitionStats
    {
        public int Hour { get; set; }
        public int Recognitions { get; set; }
        public int Detections { get; set; }
    }

    public class UserRecognitionCount
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int RecognitionCount { get; set; }
        public double AverageSimilarity { get; set; }
    }

    public class DepartmentStats
    {
        public string Department { get; set; }
        public int RecognitionCount { get; set; }
        public int UserCount { get; set; }
    }

    public class ReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportType { get; set; } // "daily", "weekly", "monthly", "custom"
        public string GroupBy { get; set; } // "hour", "day", "user", "department"
    }
}
