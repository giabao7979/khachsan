using HotelBK.Areas.Admin.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class RolesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
