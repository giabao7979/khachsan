using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
