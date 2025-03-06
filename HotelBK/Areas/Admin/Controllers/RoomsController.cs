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
            try
            {
                // Sửa "RoomTypes" thành "RoomType" nếu tên bảng trong DB là RoomType
                ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "RoomTypeID", "TypeName");
                return PartialView("_Create", new Room());
            }
            catch (Exception ex)
            {
                // Log lỗi để debug
                Console.WriteLine($"Error loading room types: {ex.Message}");
                ViewBag.RoomTypes = new SelectList(new List<RoomType>(), "RoomTypeID", "TypeName");
                return PartialView("_Create", new Room());
            }
        }
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            try
            {
                // Sửa "RoomTypes" thành "RoomType" nếu tên bảng trong DB là RoomType
                ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "RoomTypeID", "TypeName");
                return PartialView("_Create", room);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading room types: {ex.Message}");
                ViewBag.RoomTypes = new SelectList(new List<RoomType>(), "RoomTypeID", "TypeName");
                return PartialView("_Create", room);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Room room, IFormFile imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest($"Dữ liệu không hợp lệ: {errors}");
                }

                // Ghi log thông tin phòng
                System.Diagnostics.Debug.WriteLine($"Đang xử lý phòng: ID={room.RoomID}, Tên={room.RoomName}, LoạiID={room.RoomTypeID}");

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
                    System.Diagnostics.Debug.WriteLine("Đang thêm phòng mới");
                    _context.Rooms.Add(room);
                }
                else
                {
                    // Cập nhật
                    System.Diagnostics.Debug.WriteLine($"Đang cập nhật phòng ID={room.RoomID}");
                    _context.Rooms.Update(room);
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Lưu thành công");
                return Ok();
            }
            catch (Exception ex)
            {
                // Log thông tin lỗi chi tiết
                System.Diagnostics.Debug.WriteLine($"Lỗi trong CreateOrUpdate: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return BadRequest($"Lỗi khi lưu phòng: {ex.Message}");
            }
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