﻿using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace HotelBK.Controllers
{
    public class RegisterController : Controller
    {
        private readonly HotelContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public RegisterController(HotelContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string fullName, string email, string phone, string password, string confirmPassword)
        {
            // Kiểm tra dữ liệu nhập vào
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View();
            }

            // Kiểm tra độ dài và định dạng email
            if (!IsValidEmail(email))
            {
                ModelState.AddModelError("", "Email không hợp lệ");
                return View();
            }

            // Kiểm tra số điện thoại
            if (!IsValidPhoneNumber(phone))
            {
                ModelState.AddModelError("", "Số điện thoại không hợp lệ");
                return View();
            }

            // Kiểm tra độ mạnh của mật khẩu
            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                return View();
            }

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _context.Users.AnyAsync(u => u.Email == email);
            if (existingUser)
            {
                ModelState.AddModelError("", "Email đã được sử dụng");
                return View();
            }

            try
            {
                // Tạo tài khoản mới
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                if (customerRole == null)
                {
                    // Tạo vai trò Customer nếu chưa có
                    customerRole = new Role { RoleName = "Customer" };
                    _context.Roles.Add(customerRole);
                    await _context.SaveChangesAsync();
                }

                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    RoleID = customerRole.RoleID,
                    CreatedAt = DateTime.Now
                };

                // Mã hóa mật khẩu
                newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đăng ký: {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.");
                return View();
            }
        }

        // Phương thức kiểm tra email hợp lệ
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức kiểm tra số điện thoại hợp lệ
        private bool IsValidPhoneNumber(string phone)
        {
            // Cho phép số điện thoại từ 10-15 ký tự và chỉ chứa số
            return !string.IsNullOrEmpty(phone) &&
                   phone.Length >= 10 &&
                   phone.Length <= 15 &&
                   System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d+$");
        }
    }
}