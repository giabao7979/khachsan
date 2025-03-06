using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
