using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
