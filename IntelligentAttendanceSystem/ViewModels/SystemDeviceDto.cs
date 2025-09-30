namespace IntelligentAttendanceSystem.ViewModels
{
    public class SystemDeviceDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string IPVersion { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string SubnetMask { get; set; }
        public string GatewayMacAddress { get; set; }
        public string DeviceType { get; set; }
        public string DetailType { get; set; }
        public int? HttpPort { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
