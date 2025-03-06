using HotelBK.Areas.Admin.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
