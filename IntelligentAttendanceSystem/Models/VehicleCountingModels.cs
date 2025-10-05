namespace IntelligentAttendanceSystem.Models
{
    // Models/VehicleCountingModels.cs
    public class VehicleCount
    {
        public int Id { get; set; }
        public DateTime CountDate { get; set; }
        public string TimePeriod { get; set; } // "Hourly", "Daily", "Weekly"
        public string VehicleType { get; set; }
        public string Direction { get; set; }
        public int Count { get; set; }
        public int JunctionId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class VehicleCountingStats
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalVehicles { get; set; }
        public Dictionary<string, int> VehicleTypeCounts { get; set; } = new();
        public Dictionary<string, int> DirectionCounts { get; set; } = new();
        public Dictionary<string, Dictionary<string, int>> TypeDirectionMatrix { get; set; } = new();
    }
    public class VehicleDetectionEvent
    {
        public string EventId { get; set; }
        public DateTime EventTime { get; set; }
        public int JunctionId { get; set; }
        public string VehicleType { get; set; }
        public string Direction { get; set; }
        public int VehicleSize { get; set; }
        public string PlateNumber { get; set; }
        public int Speed { get; set; }
        public int Confidence { get; set; }
        public string ConfidenceLevel { get; set; }
        public int ObjectId { get; set; }
        public Rect BoundingBox { get; set; }
        public Dictionary<string, object> ObjectAttributes { get; set; } = new();
    }
}
