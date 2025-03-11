using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class UserController : Controller
    {
        private readonly HotelContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserController(HotelContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleID", "RoleName");
            return PartialView("_Create", new User());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleID", "RoleName");
            return PartialView("_Edit", user);
        }

        public async Task<IActionResult> View(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            return PartialView("_View", user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(User user, string password = null)
        {
            try
            {
                // Ghi log thông tin user được gửi lên
                System.Diagnostics.Debug.WriteLine($"Đã nhận request CreateOrUpdate User");
                System.Diagnostics.Debug.WriteLine($"User ID: {user.UserID}");
                System.Diagnostics.Debug.WriteLine($"User Name: {user.FullName}");
                System.Diagnostics.Debug.WriteLine($"User Email: {user.Email}");

                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(user.FullName))
                {
                    return BadRequest("Họ tên không được để trống");
                }

                if (string.IsNullOrEmpty(user.Email))
                {
                    return BadRequest("Email không được để trống");
                }

                if (string.IsNullOrEmpty(user.Phone))
                {
                    return BadRequest("Số điện thoại không được để trống");
                }

                // Kiểm tra email đã tồn tại chưa (trừ trường hợp cập nhật chính user đó)
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email && u.UserID != user.UserID);
                if (existingUser != null)
                {
                    return BadRequest("Email đã được sử dụng cho tài khoản khác");
                }

                if (user.UserID == 0)
                {
                    // Thêm mới
                    if (string.IsNullOrEmpty(password))
                    {
                        return BadRequest("Mật khẩu không được để trống khi tạo người dùng mới");
                    }

                    // Mã hóa mật khẩu
                    user.PasswordHash = _passwordHasher.HashPassword(user, password);
                    user.CreatedAt = DateTime.Now;

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine("Thêm user thành công");
                }
                else
                {
                    // Cập nhật
                    var existingRecord = await _context.Users.FindAsync(user.UserID);
                    if (existingRecord == null)
                    {
                        return NotFound("Không tìm thấy người dùng");
                    }

                    // Cập nhật thông tin
                    existingRecord.FullName = user.FullName;
                    existingRecord.Email = user.Email;
                    existingRecord.Phone = user.Phone;
                    existingRecord.RoleID = user.RoleID;

                    // Cập nhật mật khẩu nếu có
                    if (!string.IsNullOrEmpty(password))
                    {
                        existingRecord.PasswordHash = _passwordHasher.HashPassword(existingRecord, password);
                    }

                    _context.Users.Update(existingRecord);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine("Cập nhật user thành công");
                }

                // Phản hồi dựa trên loại request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Lưu người dùng thành công" });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi: {ex.Message}");
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Kiểm tra nếu là admin duy nhất thì không được xóa
                if (user.RoleID == 1)
                {
                    var adminCount = await _context.Users.CountAsync(u => u.RoleID == 1);
                    if (adminCount <= 1)
                    {
                        return BadRequest("Không thể xóa tài khoản admin duy nhất");
                    }
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }
    }
}