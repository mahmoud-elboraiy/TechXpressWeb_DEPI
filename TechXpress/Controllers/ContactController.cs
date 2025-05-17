using Microsoft.AspNetCore.Mvc;

namespace TechXpress.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Contact()
        {
            return View();
        }
    }
}
