using Microsoft.AspNetCore.Mvc;

namespace TechXpress.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Pay()
        {
            return View();
        }
    }
}
