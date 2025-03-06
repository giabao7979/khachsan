using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class RoomController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
