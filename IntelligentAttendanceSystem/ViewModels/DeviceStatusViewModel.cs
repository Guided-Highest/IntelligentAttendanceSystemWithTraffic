using NetSDKCS;

namespace IntelligentAttendanceSystem.ViewModels
{
    public class DeviceStatusViewModel
    {
        public bool IsConnected { get; set; }
        public IntPtr LoginID { get; set; }
        public string DeviceIP { get; set; }
        public NET_DEVICEINFO_Ex DeviceInfo { get; set; }
        public DateTime LastChecked { get; set; }

        // Helper properties for display
        public string LoginIDDisplay => LoginID.ToString();
        public string ConnectionStatus => IsConnected ? "Connected" : "Disconnected";
        public string StatusClass => IsConnected ? "text-success" : "text-danger";
    }
}
