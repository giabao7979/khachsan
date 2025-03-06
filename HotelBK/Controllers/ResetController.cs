using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Controllers
{
    public class ResetController : Controller
    {
        private readonly HotelContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public ResetController(HotelContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: /Reset/Admin
        public async Task<IActionResult> Admin()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
            if (user == null)
            {
                return Content("Không tìm thấy tài khoản admin!");
            }

            // Reset mật khẩu thành "Admin@123"
            user.PasswordHash = _passwordHasher.HashPassword(user, "Admin@123");
            _context.Update(user);
            await _context.SaveChangesAsync();

            return Content("Đã reset mật khẩu admin thành công! Mật khẩu mới: Admin@123");
        }
    }
}