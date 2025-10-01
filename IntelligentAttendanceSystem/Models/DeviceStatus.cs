using NetSDKCS;

namespace IntelligentAttendanceSystem.Models
{
    public class DeviceStatus
    {
        public bool IsConnected { get; set; }
        public DateTime LastConnected { get; set; }
        public string StatusMessage { get; set; }
        public string DeviceIP { get; set; }
        public IntPtr LoginID { get; set; }
        public NET_DEVICEINFO_Ex DeviceInfo { get; set; }
    }
}
