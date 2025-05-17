using Microsoft.AspNetCore.Mvc;

namespace TechXpress.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult AboutUs()
        {
            return View();
        }
    }
}
