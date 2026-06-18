using Microsoft.AspNetCore.Mvc;

namespace webxemphim.Controllers
{
    public class CountryController : Controller
    {
        public IActionResult Countries()
        {
            ViewData["Title"] = "Quốc gia";
            return View();
        }
    }
} 