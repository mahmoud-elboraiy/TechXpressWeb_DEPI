using Microsoft.AspNetCore.Mvc;

namespace TechXpress.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
