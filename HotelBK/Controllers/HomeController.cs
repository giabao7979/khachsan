using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Booking()
        {
            return View();
        }
    }
}
