using Microsoft.AspNetCore.Mvc;

namespace IntelligentAttendanceSystem.Controllers
{
    public class ODController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Index_Copy()
        {
            return View("Index - Copy");
        }
    }
}
