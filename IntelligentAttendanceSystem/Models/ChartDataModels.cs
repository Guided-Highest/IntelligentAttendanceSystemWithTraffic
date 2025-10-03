namespace IntelligentAttendanceSystem.Models
{
    public class ChartData
    {
        public string Period { get; set; } = string.Empty;
        public List<ChartDataPoint> DataPoints { get; set; } = new();
        public int TotalRecognitions { get; set; }
        public double AverageSimilarity { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public double AverageSimilarity { get; set; }
    }

    public class UserRecognitionChartData
    {
        public List<UserRecognitionStat> UserStats { get; set; } = new();
        public string Period { get; set; } = string.Empty;
    }

    public class UserRecognitionStat
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int RecognitionCount { get; set; }
        public double AverageSimilarity { get; set; }
    }
}
