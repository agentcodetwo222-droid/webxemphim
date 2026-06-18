using Microsoft.AspNetCore.Mvc;

namespace webxemphim.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string q)
        {
            ViewData["SearchQuery"] = q ?? "";
            return View();
        }

        [HttpPost]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("Index");
            }
            
            return RedirectToAction("Index", new { q = query });
        }
    }
} 