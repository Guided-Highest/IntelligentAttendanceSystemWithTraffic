namespace IntelligentAttendanceSystem.Models
{
    public class DashboardStats
    {
        public int TodayRecognitions { get; set; }
        public int WeekRecognitions { get; set; }
        public int TotalUsers { get; set; }
        public double AverageSimilarityToday { get; set; }
    }
}
