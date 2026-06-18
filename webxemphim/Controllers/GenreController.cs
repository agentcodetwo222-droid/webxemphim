using Microsoft.AspNetCore.Mvc;

namespace webxemphim.Controllers
{
    public class GenreController : Controller
    {
        public IActionResult Genres()
        {
            ViewData["Title"] = "Thể loại";
            return View();
        }
    }
} 