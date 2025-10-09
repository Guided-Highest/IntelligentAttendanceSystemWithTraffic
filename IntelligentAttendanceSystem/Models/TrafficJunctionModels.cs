namespace IntelligentAttendanceSystem.Models
{// Models/TrafficJunctionModels.cs
    public class TrafficJunctionEvent
    {
        public string EventId { get; set; }
        public string EventType { get; set; } = "TRAFFICJUNCTION";
        public DateTime EventTime { get; set; }
        public string GlobalImageBase64 { get; set; }
        public TrafficVehicleInfo VehicleInfo { get; set; }
        public TrafficViolationInfo ViolationInfo { get; set; }
        public JunctionInfo JunctionInfo { get; set; }
        public int EventNumber { get; set; }
        public int ChannelId { get; set; }
        public string EventAction { get; set; }
        public int SourceChannel { get; set; }
    }

    public class TrafficVehicleInfo
    {
        public string VehicleType { get; set; }
        public string Color { get; set; }
        public string PlateNumber { get; set; }
        public int Speed { get; set; }
        public string Direction { get; set; }
        public Rect VehicleRect { get; set; }
        public string VehicleImageBase64 { get; set; }
        public string PlateImageBase64 { get; set; }
        public string ObjectImageBase64 { get; set; } // Add object image

        // Additional fields
        public bool DriverSeatBelt { get; set; }
        public bool PassengerSeatBelt { get; set; }
        public string VehiclePosture { get; set; }
        public uint VehicleSignConfidence { get; set; }
        public uint VehicleCategoryConfidence { get; set; }
        public int ObjectConfidence { get; set; }
        public Dictionary<string, object> ObjectAttributes { get; set; } = new();
        public NonMotorAttributes NonMotorAttributes { get; set; }
    }
    public class NonMotorAttributes
    {
        public bool HasBag { get; set; }
        public bool HasUmbrella { get; set; }
        public bool HasCarrierBag { get; set; }
        public bool HasHat { get; set; }
        public bool HasHelmet { get; set; }
        public string Sex { get; set; }
        public byte Age { get; set; }
        public string UpperBodyColor { get; set; }
        public string LowerBodyColor { get; set; }
        public string UpperClothesType { get; set; }
        public string LowerClothesType { get; set; }
    }

    public class TrafficViolationInfo
    {
        public string ViolationType { get; set; } // "RUN_RED_LIGHT", "OVER_SPEED", etc.
        public string Description { get; set; }
        public float Confidence { get; set; }
        public DateTime ViolationTime { get; set; }
        public string LaneNumber { get; set; }
    }

    public class JunctionInfo
    {
        public string JunctionId { get; set; }
        public string JunctionName { get; set; }
        public string TrafficLightState { get; set; } // "RED", "GREEN", "YELLOW"
        public DateTime LightChangeTime { get; set; }
    }

    public class Rect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    public class TrafficRecord
    {
        public int Id { get; set; }
        public string EventId { get; set; }
        public string VehicleType { get; set; }
        public string PlateNumber { get; set; }
        public string Color { get; set; }
        public float Speed { get; set; }
        public string ViolationType { get; set; }
        public string ViolationDescription { get; set; }
        public float? Confidence { get; set; }
        public string JunctionId { get; set; }
        public string LaneNumber { get; set; }
        public DateTime EventTime { get; set; }
        public string GlobalImageBase64 { get; set; }
        public string VehicleImageBase64 { get; set; }
        public string PlateImageBase64 { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
