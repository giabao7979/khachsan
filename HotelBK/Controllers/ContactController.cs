using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
