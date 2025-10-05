using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    public class TrafficMonitoringController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
