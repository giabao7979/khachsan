using HotelBK.Areas.Admin.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}





