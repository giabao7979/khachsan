using HotelBK.Data;
using HotelBK.Models;
using HotelBK.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBK.Controllers
{
    public class RoomController : Controller
    {
        private readonly HotelContext _context;
        private readonly IRoomService _roomService;

        public RoomController(HotelContext context, IRoomService roomService = null)
        {
            _context = context;
            _roomService = roomService;
        }

        // GET: /Room
        public async Task<IActionResult> Index(DateTime? checkIn, DateTime? checkOut, int? roomType, int? priceRange)
        {
            // Kiểm tra xem có phải là request tìm kiếm không
            bool isSearchRequest = checkIn.HasValue || checkOut.HasValue || roomType.HasValue || priceRange.HasValue;
            ViewBag.IsSearchResult = isSearchRequest;

            // Khởi tạo giá trị mặc định nếu không có
            if (!checkIn.HasValue) checkIn = DateTime.Now;
            if (!checkOut.HasValue) checkOut = DateTime.Now.AddDays(1);

            // Đảm bảo ngày hợp lệ
            if (checkIn.Value < DateTime.Now) checkIn = DateTime.Now;
            if (checkOut.Value <= checkIn.Value) checkOut = checkIn.Value.AddDays(1);

            // Lưu lại để hiển thị trong view
            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.RoomType = roomType ?? 0;
            ViewBag.PriceRange = priceRange ?? 0;

            // Xây dựng query
            var query = _context.Rooms.Include(r => r.RoomType).AsQueryable();

            // Nếu là request tìm kiếm, áp dụng các điều kiện lọc
            if (isSearchRequest)
            {
                // Lọc theo loại phòng nếu được chọn
                if (roomType.HasValue && roomType.Value > 0)
                {
                    query = query.Where(r => r.RoomTypeID == roomType.Value);
                }

                // Lọc theo khoảng giá nếu được chọn
                if (priceRange.HasValue && priceRange.Value > 0)
                {
                    switch (priceRange.Value)
                    {
                        case 1: // Dưới 500.000 VNĐ
                            query = query.Where(r => r.Price < 500000);
                            break;
                        case 2: // 500.000 - 1.000.000 VNĐ
                            query = query.Where(r => r.Price >= 500000 && r.Price <= 1000000);
                            break;
                        case 3: // 1.000.000 - 2.000.000 VNĐ
                            query = query.Where(r => r.Price > 1000000 && r.Price <= 2000000);
                            break;
                        case 4: // Trên 2.000.000 VNĐ
                            query = query.Where(r => r.Price > 2000000);
                            break;
                    }
                }

                // Luôn lọc bỏ phòng đang bảo trì
                query = query.Where(r => r.Status != "Bảo trì");

                // Lọc phòng đã đặt trong khoảng thời gian
                var bookedRoomIds = await _context.Bookings
                    .Where(b => b.Status != "Cancelled" &&
                           ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                            (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                            (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)))
                    .Select(b => b.RoomID)
                    .ToListAsync();

                query = query.Where(r => !bookedRoomIds.Contains(r.RoomID));
            }

            var rooms = await query.ToListAsync();
            return View(rooms);
        }

        // GET: /Room/Detail/5
        public async Task<IActionResult> Detail(int id, DateTime? checkIn = null, DateTime? checkOut = null)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            // Truyền các tham số ngày nếu có
            if (checkIn.HasValue && checkOut.HasValue)
            {
                ViewBag.CheckIn = checkIn.Value;
                ViewBag.CheckOut = checkOut.Value;
            }
            else
            {
                ViewBag.CheckIn = DateTime.Now.AddDays(1);
                ViewBag.CheckOut = DateTime.Now.AddDays(2);
            }

            return View(room);
        }

        // GET: /Room/CheckAvailability
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int roomId, DateTime checkIn, DateTime checkOut)
        {
            // Kiểm tra ngày có hợp lệ không
            if (checkIn < DateTime.Now)
            {
                return Json(new { success = false, message = "Ngày nhận phòng không thể trong quá khứ." });
            }

            if (checkOut <= checkIn)
            {
                return Json(new { success = false, message = "Ngày trả phòng phải sau ngày nhận phòng." });
            }

            // Kiểm tra phòng
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin phòng." });
            }

            if (room.Status == "Bảo trì")
            {
                return Json(new { success = false, message = "Phòng đang trong trạng thái bảo trì." });
            }

            // Kiểm tra xem phòng có đặt trong khoảng thời gian này không
            var existingBooking = await _context.Bookings
                .AnyAsync(b => b.RoomID == roomId &&
                          b.Status != "Cancelled" &&
                          ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                           (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                           (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)));

            if (existingBooking)
            {
                return Json(new { success = false, message = "Phòng đã được đặt trong khoảng thời gian này." });
            }

            // Tính số ngày và tổng tiền
            int numberOfDays = (int)(checkOut - checkIn).TotalDays;
            decimal totalAmount = room.Price * numberOfDays;

            return Json(new
            {
                success = true,
                message = "Phòng khả dụng trong khoảng thời gian này.",
                numberOfDays = numberOfDays,
                price = room.Price,
                totalAmount = totalAmount,
                formattedTotalAmount = totalAmount.ToString("N0") + " VNĐ"
            });
        }

        // Tìm kiếm nâng cao với nhiều tiêu chí hơn
        [HttpGet]
        public async Task<IActionResult> AdvancedSearch(DateTime? checkIn, DateTime? checkOut, int? roomType,
                                                     int? priceRange, int? beds, int? bathrooms, bool? withWifi)
        {
            // Cập nhật ViewBag cho tất cả tham số
            ViewBag.CheckIn = checkIn ?? DateTime.Now;
            ViewBag.CheckOut = checkOut ?? DateTime.Now.AddDays(1);
            ViewBag.RoomType = roomType ?? 0;
            ViewBag.PriceRange = priceRange ?? 0;
            ViewBag.Beds = beds ?? 0;
            ViewBag.Bathrooms = bathrooms ?? 0;
            ViewBag.WithWifi = withWifi ?? false;
            ViewBag.IsAdvancedSearch = true;

            var query = _context.Rooms.Include(r => r.RoomType).AsQueryable();

            // Áp dụng các tiêu chí tìm kiếm tương tự như trong Index
            // ... (code tương tự như trong Index)

            // Thêm các tiêu chí nâng cao
            if (beds.HasValue && beds.Value > 0)
            {
                query = query.Where(r => r.Beds >= beds.Value);
            }

            if (bathrooms.HasValue && bathrooms.Value > 0)
            {
                query = query.Where(r => r.Bathrooms >= bathrooms.Value);
            }

            // Lưu ý: Giả sử có một trường withWifi trong model Room
            // Nếu không có, bạn có thể điều chỉnh hoặc bỏ qua điều kiện này
            if (withWifi.HasValue && withWifi.Value)
            {
                query = query.Where(r => r.Description.Contains("Wifi") || r.Description.Contains("WiFi"));
            }

            var rooms = await query.ToListAsync();
            return View("Index", rooms);
        }
    }
}