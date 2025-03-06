using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelBK.Services;
using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class RoomsController : Controller
    {
        private readonly HotelContext _context;
        private readonly IRoomService _roomService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RoomsController(HotelContext context, IRoomService roomService, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _roomService = roomService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAllRooms();
            return View(rooms);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "RoomTypeID", "TypeName");
            return PartialView("_Create", new Room());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomService.GetRoomById(id);
            if (room == null)
            {
                return NotFound();
            }

            ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "RoomTypeID", "TypeName");
            return PartialView("_Create", room);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Room room, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Dữ liệu không hợp lệ!");
            }

            // Xử lý upload hình ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                // Tạo tên file duy nhất
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

                // Đảm bảo thư mục tồn tại
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "rooms");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Lưu file
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn ảnh
                room.Image = "/images/rooms/" + fileName;
            }

            if (room.RoomID == 0)
            {
                // Tạo mới
                await _roomService.CreateRoom(room);
            }
            else
            {
                // Cập nhật
                await _roomService.UpdateRoom(room);
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _roomService.DeleteRoom(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}