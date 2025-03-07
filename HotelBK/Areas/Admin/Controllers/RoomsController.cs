using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelBK.Services;
using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.IdentityModel.Tokens;

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
                var roomTypes = await _context.RoomTypes.ToListAsync();
                ViewBag.RoomTypes = new SelectList(roomTypes, "RoomTypeID", "TypeName");

                System.Diagnostics.Debug.WriteLine($"Đã tìm thấy {roomTypes.Count} loại phòng");
                return PartialView("_Create", new Room());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi: {ex.Message}");
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

            ViewBag.RoomTypes = new SelectList(
                await _context.RoomTypes.ToListAsync(),
                "RoomTypeID",
                "TypeName",
                room.RoomTypeID
            );

            // Trả về dữ liệu JSON để JavaScript có thể sử dụng
            return Json(room);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Room room, IFormFile imageFile)
        {
            try
            {
                string[] validStatuses = { "Đang ở", "Bảo trì", "Còn trống" };
                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrEmpty(room.RoomName))
                {
                    return BadRequest("Tên phòng không được để trống");
                }
                // Đảm bảo Status là một trong các giá trị được chấp nhận
                if (string.IsNullOrEmpty(room.Status) || !validStatuses.Contains(room.Status))
                {
                    room.Status = "Còn trống";
                }
                if (room.Price <= 0)
                {
                    return BadRequest("Giá phải lớn hơn 0");
                }

                if (room.Beds <= 0)
                {
                    return BadRequest("Số giường phải lớn hơn 0");
                }

                if (room.Bathrooms <= 0)
                {
                    return BadRequest("Số phòng tắm phải lớn hơn 0");
                }

                // Đặt giá trị mặc định cho các trường có thể NULL

                // Xử lý RoomTypeID
                if (room.RoomTypeID <= 0)
                {
                    // Đặt giá trị mặc định là 1 (VIP)
                    room.RoomTypeID = 1;
                }
                else
                {
                    // Kiểm tra xem RoomTypeID có tồn tại trong bảng RoomType không
                    var roomTypeExists = await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeID == room.RoomTypeID);
                    if (!roomTypeExists)
                    {
                        return BadRequest($"Loại phòng với ID {room.RoomTypeID} không tồn tại");
                    }
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

                // Ghi log thông tin phòng
                System.Diagnostics.Debug.WriteLine($"Thông tin phòng trước khi lưu:");
                System.Diagnostics.Debug.WriteLine($"ID: {room.RoomID}");
                System.Diagnostics.Debug.WriteLine($"Tên: {room.RoomName}");
                System.Diagnostics.Debug.WriteLine($"Giá: {room.Price}");
                System.Diagnostics.Debug.WriteLine($"Loại phòng ID: {room.RoomTypeID}");
                System.Diagnostics.Debug.WriteLine($"Số giường: {room.Beds}");
                System.Diagnostics.Debug.WriteLine($"Số phòng tắm: {room.Bathrooms}");
                System.Diagnostics.Debug.WriteLine($"Trạng thái: {room.Status}");

                // Lưu vào database
                if (room.RoomID == 0)
                {
                    // Thêm mới
                    System.Diagnostics.Debug.WriteLine("Đang thêm phòng mới");
                    _context.Rooms.Add(room);
                }
                else
                {
                    // Cập nhật
                    System.Diagnostics.Debug.WriteLine($"Đang cập nhật phòng ID={room.RoomID}");

                    // Tìm phòng hiện tại
                    var existingRoom = await _context.Rooms.FindAsync(room.RoomID);
                    if (existingRoom == null)
                    {
                        return BadRequest($"Không tìm thấy phòng với ID {room.RoomID}");
                    }

                    // Cập nhật các trường
                    existingRoom.RoomName = room.RoomName;
                    existingRoom.Price = room.Price;
                    existingRoom.StarRating = room.StarRating;
                    existingRoom.Beds = room.Beds;
                    existingRoom.Bathrooms = room.Bathrooms;
                    existingRoom.Description = room.Description;
                    existingRoom.Status = room.Status;
                    existingRoom.RoomTypeID = room.RoomTypeID;

                    // Chỉ cập nhật Image nếu có upload file mới
                    if (!string.IsNullOrEmpty(room.Image))
                    {
                        existingRoom.Image = room.Image;
                    }

                    _context.Rooms.Update(existingRoom);
                }

                // Lưu thay đổi
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Lưu thành công");
                return Ok();
            }
            catch (DbUpdateException dbEx)
            {
                // Xử lý lỗi Entity Framework
                System.Diagnostics.Debug.WriteLine($"DbUpdateException: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {dbEx.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {dbEx.InnerException.StackTrace}");
                    return BadRequest($"Lỗi database: {dbEx.InnerException.Message}");
                }
                return BadRequest($"Lỗi khi lưu phòng: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
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
        [HttpGet]
        public async Task<IActionResult> GetRoomTypes()
        {
            try
            {
                var roomTypes = await _context.RoomTypes.ToListAsync();
                return Json(roomTypes);
            }
            catch (Exception ex)
            {
                return Json(new List<RoomType>());
            }
        }
    }
}