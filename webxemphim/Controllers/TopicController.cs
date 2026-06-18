using Microsoft.AspNetCore.Mvc;

namespace webxemphim.Controllers
{
    public class TopicController : Controller
    {
        public IActionResult Topics()
        {
            ViewData["Title"] = "Các chủ đề";
            return View();
        }
    }
} 