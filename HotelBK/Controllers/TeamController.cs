﻿using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class TeamController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
