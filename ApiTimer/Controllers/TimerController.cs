using Microsoft.AspNetCore.Mvc;

namespace ApiTimer.Controllers
{
    public class TimerController : Controller
    {
        public IActionResult Timer()
        {
            return View();
        }
    }
}
