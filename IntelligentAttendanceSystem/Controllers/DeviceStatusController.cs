using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    public class DeviceStatusController : Controller
    {
        private readonly IDahuaDeviceService _deviceService;

        public DeviceStatusController(IDahuaDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        public IActionResult Index()
        {
            var model = new DeviceStatusViewModel
            {
                IsConnected = _deviceService.IsDeviceConnected,
                LoginID = _deviceService.LoginID,
                DeviceIP = _deviceService.DeviceIP,
                DeviceInfo = _deviceService.DeviceInfo,
                LastChecked = DateTime.Now
            };

            return View(model);
        }

        public IActionResult ApiStatus()
        {
            var status = new
            {
                IsConnected = _deviceService.IsDeviceConnected,
                LoginID = _deviceService.LoginID.ToString(),
                DeviceIP = _deviceService.DeviceIP,
                Channels = _deviceService.DeviceInfo.nChanNum, // Example property
                LastUpdate = DateTime.Now
            };

            return Json(status);
        }
    }
}
