using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
