using HotelBK.Data;
using HotelBK.Models;
using HotelBK.Services;
using Microsoft.EntityFrameworkCore; // Thêm dòng này
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Thêm dòng này

namespace HotelBK.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelContext _context;
        private readonly IBookingService _bookingService;

        public BookingController(HotelContext context, IBookingService bookingService)
        {
            _context = context;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index(int? roomId, DateTime? checkIn, DateTime? checkOut, int? adults, int? children, int? roomCount)
        {
            // Thiết lập giá trị mặc định nếu không có
            if (!checkIn.HasValue)
                checkIn = DateTime.Now.AddDays(1);

            if (!checkOut.HasValue)
                checkOut = DateTime.Now.AddDays(2);

            // Đảm bảo CheckOut sau CheckIn
            if (checkIn >= checkOut)
                checkOut = checkIn.Value.AddDays(1);

            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.Adults = adults ?? 2;
            ViewBag.Children = children ?? 0;
            ViewBag.RoomCount = roomCount ?? 1;

            if (roomId.HasValue)
            {
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomID == roomId.Value);

                if (room != null) // Kiểm tra null
                {
                    // Kiểm tra phòng có trống không
                    bool isAvailable = await _bookingService.KiemTraPhongTrong(
                        room.RoomID,
                        checkIn.Value,
                        checkOut.Value);

                    if (!isAvailable)
                    {
                        TempData["ErrorMessage"] = "Phòng này đã được đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác hoặc phòng khác.";
                        return RedirectToAction("Index", "Room");
                    }

                    ViewBag.Room = room;

                    // Tính tổng giá
                    decimal totalPrice = await _bookingService.TinhTienPhong(
                        room.RoomID,
                        checkIn.Value,
                        checkOut.Value,
                        roomCount ?? 1);

                    ViewBag.TotalNights = (checkOut.Value - checkIn.Value).Days;
                    ViewBag.TotalPrice = totalPrice;
                }
            }
            else
            {
                // Lấy danh sách phòng trống
                var availableRooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == "Còn trống")
                    .ToListAsync();

                ViewBag.AvailableRooms = availableRooms;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BookRoom(Booking model)
        {
            if (!ModelState.IsValid)
            {
                // Trả về form đặt phòng với thông báo lỗi
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomID == model.RoomID);

                ViewBag.Room = room;
                ViewBag.CheckIn = model.CheckInDate;
                ViewBag.CheckOut = model.CheckOutDate;
                ViewBag.RoomCount = model.RoomCount;

                return View("Index", model);
            }

            // Kiểm tra ngày
            if (model.CheckInDate >= model.CheckOutDate)
            {
                ModelState.AddModelError("", "Ngày nhận phòng phải trước ngày trả phòng.");
                return View("Index", model);
            }

            if (model.CheckInDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("", "Ngày nhận phòng không thể trong quá khứ.");
                return View("Index", model);
            }

            // Kiểm tra phòng có trống không
            bool isRoomAvailable = await _bookingService.KiemTraPhongTrong(
                model.RoomID,
                model.CheckInDate,
                model.CheckOutDate);

            if (!isRoomAvailable)
            {
                ModelState.AddModelError("", "Phòng này đã được đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác hoặc phòng khác.");
                return View("Index", model);
            }

            // Thiết lập trạng thái và ngày tạo
            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;

            // Nếu người dùng đã đăng nhập, sử dụng thông tin từ tài khoản
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userIdInt))
                {
                    var user = await _context.Users.FindAsync(userIdInt);
                    if (user != null)
                    {
                        // Chỉ ghi đè nếu người dùng không cung cấp thông tin tùy chỉnh
                        if (string.IsNullOrEmpty(model.FullName))
                            model.FullName = user.FullName;

                        if (string.IsNullOrEmpty(model.Email))
                            model.Email = user.Email;

                        if (string.IsNullOrEmpty(model.Phone))
                            model.Phone = user.Phone;
                    }
                }
            }

            // Tạo đặt phòng
            var booking = await _bookingService.TaoDatPhong(model);

            if (booking != null && booking.BookingID > 0)
            {
                TempData["SuccessMessage"] = "Đặt phòng thành công! Mã đặt phòng của bạn là: " + booking.BookingID;
                return RedirectToAction("Confirmation", new { id = booking.BookingID });
            }
            else
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt phòng. Vui lòng thử lại sau.");
                return View("Index", model);
            }
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Tính tổng tiền
            var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
            if (totalDays < 1) totalDays = 1;

            ViewBag.TotalDays = totalDays;
            ViewBag.TotalAmount = totalDays * booking.Room.Price * booking.RoomCount;

            return View(booking);
        }

        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Login");

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => b.Email == email)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Login");

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingID == id && b.Email == email);

            if (booking == null)
                return NotFound();

            // Chỉ cho phép hủy đặt phòng ở trạng thái đang chờ hoặc đã xác nhận
            if (booking.Status != "Pending" && booking.Status != "Confirmed")
            {
                TempData["ErrorMessage"] = "Không thể hủy đặt phòng ở trạng thái này.";
                return RedirectToAction("MyBookings");
            }

            // Kiểm tra nếu hủy trong vòng 24 giờ trước khi nhận phòng
            if (booking.CheckInDate.Subtract(DateTime.Now).TotalHours < 24)
            {
                TempData["ErrorMessage"] = "Không thể hủy đặt phòng trong vòng 24 giờ trước khi nhận phòng.";
                return RedirectToAction("MyBookings");
            }

            // Cập nhật trạng thái đặt phòng
            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);

            // Kiểm tra xem trạng thái phòng có cần cập nhật không
            if (booking.Room.Status == "Đang ở")
            {
                var activeBookings = await _context.Bookings.CountAsync(b =>
                    b.RoomID == booking.RoomID &&
                    b.Status == "Checked-in" &&
                    b.BookingID != booking.BookingID);

                if (activeBookings == 0)
                {
                    booking.Room.Status = "Còn trống";
                    _context.Rooms.Update(booking.Room);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt phòng đã được hủy thành công.";
            return RedirectToAction("MyBookings");
        }
    }
}