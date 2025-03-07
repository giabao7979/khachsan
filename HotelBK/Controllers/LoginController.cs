using System.Security.Claims;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Controllers
{
    public class LoginController : Controller
    {
        private readonly HotelContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public LoginController(HotelContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại");
                return View();
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (passwordResult == PasswordVerificationResult.Success)
            {
                // Chuyển hướng theo vai trò
                if (user.Role.RoleName == "Admin")
                {
                    return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Mật khẩu không chính xác");
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}