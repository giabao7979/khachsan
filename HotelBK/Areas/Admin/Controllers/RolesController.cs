using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class RolesController : Controller
    {
        private readonly HotelContext _context;

        public RolesController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        public IActionResult Create()
        {
            return PartialView("_Create", new Role());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return PartialView("_Edit", role);
        }

        public async Task<IActionResult> View(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Lấy số lượng người dùng có vai trò này
            var userCount = await _context.Users.CountAsync(u => u.RoleID == id);
            ViewBag.UserCount = userCount;

            return PartialView("_View", role);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Role role)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(role.RoleName))
                {
                    return BadRequest("Tên vai trò không được để trống");
                }

                // Kiểm tra tên vai trò đã tồn tại chưa
                var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == role.RoleName && r.RoleID != role.RoleID);
                if (existingRole != null)
                {
                    return BadRequest("Tên vai trò đã tồn tại");
                }

                if (role.RoleID == 0)
                {
                    // Thêm mới
                    _context.Roles.Add(role);
                }
                else
                {
                    // Cập nhật
                    var existingRecord = await _context.Roles.FindAsync(role.RoleID);
                    if (existingRecord == null)
                    {
                        return NotFound("Không tìm thấy vai trò");
                    }

                    // Không cho phép sửa vai trò mặc định
                    if (existingRecord.RoleID <= 3)
                    {
                        return BadRequest("Không thể sửa vai trò mặc định của hệ thống");
                    }

                    existingRecord.RoleName = role.RoleName;
                    _context.Roles.Update(existingRecord);
                }

                await _context.SaveChangesAsync();

                // Phản hồi dựa trên loại request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Lưu vai trò thành công" });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound();
                }

                // Không cho phép xóa vai trò mặc định
                if (id <= 3)
                {
                    return BadRequest("Không thể xóa vai trò mặc định của hệ thống");
                }

                // Kiểm tra xem có người dùng nào đang sử dụng vai trò này không
                var userWithThisRole = await _context.Users.AnyAsync(u => u.RoleID == id);
                if (userWithThisRole)
                {
                    return BadRequest("Không thể xóa vai trò đang được sử dụng bởi người dùng");
                }

                _context.Roles.Remove(role);
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