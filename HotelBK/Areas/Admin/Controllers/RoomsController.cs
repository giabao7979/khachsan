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
        private readonly IRoomImageService _roomImageService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RoomsController(HotelContext context, IRoomService roomService, IRoomImageService roomImageService, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _roomService = roomService;
            _roomImageService = roomImageService;
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

        public async Task<IActionResult> View(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            return PartialView("_View", room);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            // Ghi log để debug
            System.Diagnostics.Debug.WriteLine($"Edit: Tìm thấy phòng ID={id}, Tên={room.RoomName}");

            ViewBag.RoomTypes = new SelectList(
                await _context.RoomTypes.ToListAsync(),
                "RoomTypeID",
                "TypeName",
                room.RoomTypeID
            );

            return PartialView("_Edit", room);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Room room, IFormFile imageFile, List<IFormFile> additionalImages)
        {
            try
            {
                // Ghi log thông tin phòng được gửi lên
                System.Diagnostics.Debug.WriteLine($"Đã nhận request CreateOrUpdate");
                System.Diagnostics.Debug.WriteLine($"Room ID: {room.RoomID}");
                System.Diagnostics.Debug.WriteLine($"Room Name: {room.RoomName}");
                System.Diagnostics.Debug.WriteLine($"Room Type ID: {room.RoomTypeID}");
                System.Diagnostics.Debug.WriteLine($"Image file: {(imageFile != null ? imageFile.FileName : "null")}");
                System.Diagnostics.Debug.WriteLine($"Additional images count: {(additionalImages != null ? additionalImages.Count : 0)}");

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
                    await _context.SaveChangesAsync();

                    // Trả về mã JavaScript để tạo thông báo cho phòng mới
                    TempData["NewRoomNotification"] = $"{{ \"roomId\": {room.RoomID}, \"roomName\": \"{room.RoomName}\" }}";
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

                // Nếu có ảnh chính mới, thêm vào RoomImages
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Hủy tất cả ảnh chính hiện tại
                    var currentMainImages = await _context.RoomImages
                        .Where(ri => ri.RoomID == room.RoomID && ri.IsMainImage)
                        .ToListAsync();

                    foreach (var mainImage in currentMainImages)
                    {
                        mainImage.IsMainImage = false;
                    }

                    // Thêm ảnh chính mới
                    var newMainImage = new RoomImage
                    {
                        RoomID = room.RoomID,
                        ImagePath = room.Image,
                        IsMainImage = true,
                        DisplayOrder = 0
                    };

                    _context.RoomImages.Add(newMainImage);
                    await _context.SaveChangesAsync();
                }
                // Xử lý các ảnh bổ sung
                if (additionalImages != null && additionalImages.Count > 0)
                {
                    foreach (var additionalImage in additionalImages)
                    {
                        if (additionalImage.Length > 0)
                        {
                            // Tạo tên file duy nhất cho mỗi ảnh
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(additionalImage.FileName);

                            // Sử dụng thư mục tương tự với ảnh chính
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "rooms");

                            // Lưu file
                            string filePath = Path.Combine(uploadsFolder, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await additionalImage.CopyToAsync(stream);
                            }

                            // Tạo bản ghi RoomImage mới
                            var roomImage = new RoomImage
                            {
                                RoomID = room.RoomID,
                                ImagePath = "/images/rooms/" + fileName,
                                IsMainImage = false,
                                DisplayOrder = await _context.RoomImages
                                    .Where(ri => ri.RoomID == room.RoomID)
                                    .CountAsync() // Đặt thứ tự hiển thị bằng số lượng ảnh hiện có
                            };

                            // Thêm vào database
                            _context.RoomImages.Add(roomImage);
                        }
                    }

                    // Lưu các ảnh bổ sung
                    await _context.SaveChangesAsync();
                }
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // AJAX request
                    return Ok(new { success = true, message = "Lưu phòng thành công" });
                }
                else
                {
                    // Normal form submit
                    return RedirectToAction("Index");
                }
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
                System.Diagnostics.Debug.WriteLine($"Lỗi lấy danh sách loại phòng: {ex.Message}");
                return Json(new List<RoomType>());
            }
        }
        #region Room Images Management

        // Hiển thị trang quản lý ảnh của phòng
        public async Task<IActionResult> ManageImages(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // Thêm ảnh mới cho phòng
        [HttpPost]
        public async Task<IActionResult> AddImage(int roomId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("Vui lòng chọn một tệp ảnh");
            }

            var roomImage = new RoomImage
            {
                RoomID = roomId
            };

            var result = await _roomImageService.AddRoomImage(roomImage, imageFile);

            if (result != null)
            {
                return Ok(new { success = true, image = result });
            }
            else
            {
                return BadRequest("Không thể thêm ảnh. Vui lòng thử lại");
            }
        }

        // Xóa một ảnh
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                // Log để debug
                System.Diagnostics.Debug.WriteLine($"Đang xóa ảnh với ID: {id}");

                // Lấy thông tin ảnh
                var roomImage = await _context.RoomImages.FindAsync(id);
                if (roomImage == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy ảnh với ID: {id}");
                    return Json(new { success = false, message = "Không tìm thấy ảnh" });
                }

                // Lưu thông tin để log
                int roomId = roomImage.RoomID;
                string imagePath = roomImage.ImagePath;
                System.Diagnostics.Debug.WriteLine($"Tìm thấy ảnh: ID={id}, RoomID={roomId}, Path={imagePath}");

                // Xóa file vật lý nếu tồn tại
                if (!string.IsNullOrEmpty(imagePath))
                {
                    string webRootPath = _hostEnvironment.WebRootPath;
                    string filePath = Path.Combine(webRootPath, imagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        System.Diagnostics.Debug.WriteLine($"Đã xóa file: {filePath}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Không tìm thấy file: {filePath}");
                    }
                }

                // Xóa bản ghi trong database
                _context.RoomImages.Remove(roomImage);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Đã xóa bản ghi RoomImage với ID: {id}");

                return Json(new { success = true, message = "Đã xóa ảnh thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi xóa ảnh: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Cập nhật thứ tự hiển thị của ảnh
        [HttpPost]
        public async Task<IActionResult> UpdateImageOrder(int id, int newOrder)
        {
            var success = await _roomImageService.UpdateDisplayOrder(id, newOrder);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { success = true });
        }

        #endregion
    }
}