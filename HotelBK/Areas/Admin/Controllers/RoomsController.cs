using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomsController : Controller
    {
        private readonly HotelContext _context;

        public RoomsController(HotelContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách phòng
        public IActionResult Index()
        {
            var rooms = _context.Rooms.ToList();
            return View(rooms);
        }

        // Hiển thị form thêm phòng (AJAX)
        public IActionResult Create()
        {
            return PartialView("_Create", new Room());
        }

        // Hiển thị form sửa phòng (AJAX)
        public IActionResult Edit(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
            {
                return NotFound();
            }
            return PartialView("_Create", room);
        }

        // Xử lý Thêm hoặc Cập nhật phòng (AJAX)
        [HttpPost]
        public IActionResult CreateOrUpdate(Room room)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Dữ liệu không hợp lệ!");
            }

            if (room.RoomID == 0)
            {
                _context.Rooms.Add(room);
            }
            else
            {
                _context.Rooms.Update(room);
            }

            _context.SaveChanges();
            return Ok();
        }

        // Xóa phòng bằng AJAX
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
            {
                return NotFound();
            }

            _context.Rooms.Remove(room);
            _context.SaveChanges();
            return Ok();
        }
    }
}
