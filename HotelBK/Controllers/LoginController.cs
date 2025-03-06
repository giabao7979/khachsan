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
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu");
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác");
                return View();
            }

            // Kiểm tra mật khẩu (bạn cần cập nhật cách lưu mật khẩu)
            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác");
                return View();
            }

            // Tạo claims cho người dùng đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Chuyển hướng dựa trên vai trò
            if (user.Role.RoleName == "Admin")
            {
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}