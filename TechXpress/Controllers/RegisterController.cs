using Microsoft.AspNetCore.Mvc;

namespace TechXpress.Controllers
{
    public class RegisterController : Controller
    {
        public IActionResult Register()
        {
            return View();
        }
    }
}
