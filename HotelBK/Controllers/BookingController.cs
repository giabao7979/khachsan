using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelBK.Services;
using HotelBK.Data;
using HotelBK.Models;
using System.Security.Claims;

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

        [HttpGet]
        public async Task<IActionResult> Index(int? roomId, DateTime? checkIn, DateTime? checkOut, int? adults, int? children, int? roomCount)
        {
            // Ghi log để debug
            System.Diagnostics.Debug.WriteLine($"Received roomId parameter: {roomId}");

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

            // Tạo đối tượng Booking mới để dùng cho view
            var model = new Booking
            {
                CheckInDate = checkIn.Value,
                CheckOutDate = checkOut.Value,
                RoomCount = roomCount ?? 1,
                SpecialRequest = string.Empty // Khởi tạo với chuỗi rỗng
            };

            // Đảm bảo RoomID được gán đúng
            if (roomId.HasValue && roomId.Value > 0)
            {
                model.RoomID = roomId.Value;
                System.Diagnostics.Debug.WriteLine($"Set model.RoomID to {model.RoomID}");

                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomID == roomId.Value);

                if (room != null)
                {
                    // Kiểm tra nếu phòng đang "Bảo trì"
                    if (room.Status == "Bảo trì")
                    {
                        TempData["ErrorMessage"] = "Phòng này đang trong quá trình bảo trì và không thể đặt.";
                        return RedirectToAction("Index", "Room");
                    }

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
                System.Diagnostics.Debug.WriteLine("Warning: roomId parameter is missing or invalid");
                TempData["ErrorMessage"] = "Vui lòng chọn phòng trước khi đặt";
                return RedirectToAction("Index", "Room");
            }

            // Nếu người dùng đã đăng nhập, lấy thông tin từ tài khoản
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                model.FullName = User.Identity.Name;
                model.Email = User.FindFirstValue(ClaimTypes.Email);

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userIdInt))
                {
                    var user = await _context.Users.FindAsync(userIdInt);
                    if (user != null)
                    {
                        model.Phone = user.Phone;
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BookRoom(Booking model)
        {
            // Log để debug
            System.Diagnostics.Debug.WriteLine($"RoomID in form: {model.RoomID}");
            if (model.RoomID <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn phòng trước khi đặt";
                return RedirectToAction("Index", "Room");
            }
            // Đảm bảo SpecialRequest không null
            if (model.SpecialRequest == null)
            {
                model.SpecialRequest = string.Empty;
            }

            // Khai báo biến ở đầu phương thức để tránh lỗi scope
            List<string> errorMessages = new List<string>();

            // Hiển thị chi tiết lỗi ModelState để debug
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState is invalid. Details:");

                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {state.Key}, Error: {error.ErrorMessage}");
                        errorMessages.Add($"{state.Key}: {error.ErrorMessage}");
                    }
                }

                // Lấy lại thông tin phòng
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomID == model.RoomID);

                ViewBag.Room = room;
                ViewBag.CheckIn = model.CheckInDate;
                ViewBag.CheckOut = model.CheckOutDate;
                ViewBag.RoomCount = model.RoomCount;

                // Hiển thị thông báo lỗi cụ thể hơn
                if (errorMessages.Any())
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin đặt phòng: " + string.Join(", ", errorMessages);
                }
                else
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin đặt phòng.";
                }

                return View("Index", model);
            }

            try
            {
                // Kiểm tra RoomID
                if (model.RoomID <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn phòng";
                    return RedirectToAction("Index", "Room");
                }

                // Kiểm tra ngày
                if (model.CheckInDate >= model.CheckOutDate)
                {
                    ModelState.AddModelError("", "Ngày nhận phòng phải trước ngày trả phòng.");
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.RoomID == model.RoomID);
                    ViewBag.Room = room;
                    return View("Index", model);
                }

                if (model.CheckInDate.Date < DateTime.Now.Date)
                {
                    ModelState.AddModelError("", "Ngày nhận phòng không thể trong quá khứ.");
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.RoomID == model.RoomID);
                    ViewBag.Room = room;
                    return View("Index", model);
                }

                // Kiểm tra phòng có tồn tại không
                var roomExists = await _context.Rooms.AnyAsync(r => r.RoomID == model.RoomID);
                if (!roomExists)
                {
                    TempData["ErrorMessage"] = "Phòng không tồn tại.";
                    return RedirectToAction("Index", "Room");
                }

                // Kiểm tra trạng thái phòng
                var roomToBook = await _context.Rooms.FindAsync(model.RoomID);
                if (roomToBook.Status == "Bảo trì")
                {
                    TempData["ErrorMessage"] = "Không thể đặt phòng đang bảo trì.";
                    return RedirectToAction("Detail", "Room", new { id = model.RoomID });
                }

                // Kiểm tra phòng có trống không
                bool isRoomAvailable = await _bookingService.KiemTraPhongTrong(
                    model.RoomID,
                    model.CheckInDate,
                    model.CheckOutDate);

                if (!isRoomAvailable)
                {
                    TempData["ErrorMessage"] = "Phòng này đã được đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác hoặc phòng khác.";
                    return RedirectToAction("Detail", "Room", new { id = model.RoomID });
                }

                // Đảm bảo các trường không null
                if (string.IsNullOrWhiteSpace(model.FullName) ||
                    string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.Phone))
                {
                    // Nếu người dùng đã đăng nhập, lấy thông tin từ tài khoản
                    if (User.Identity != null && User.Identity.IsAuthenticated)
                    {
                        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userIdInt))
                        {
                            var user = await _context.Users.FindAsync(userIdInt);
                            if (user != null)
                            {
                                if (string.IsNullOrWhiteSpace(model.FullName))
                                    model.FullName = user.FullName;

                                if (string.IsNullOrWhiteSpace(model.Email))
                                    model.Email = user.Email;

                                if (string.IsNullOrWhiteSpace(model.Phone))
                                    model.Phone = user.Phone;
                            }
                        }
                    }
                }

                // Kiểm tra lại sau khi lấy thông tin từ user
                if (string.IsNullOrWhiteSpace(model.FullName) ||
                    string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.Phone))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin cá nhân.";
                    return RedirectToAction("Detail", "Room", new { id = model.RoomID });
                }

                // Xác định số người nếu chưa có
                if (model.RoomCount <= 0)
                {
                    model.RoomCount = 1;
                }

                // Thiết lập trạng thái và ngày tạo
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;

                // Tạo đặt phòng
                _context.Bookings.Add(model);
                await _context.SaveChangesAsync();

                if (model.BookingID > 0)
                {
                    TempData["SuccessMessage"] = "Đặt phòng thành công! Mã đặt phòng của bạn là: " + model.BookingID;
                    return RedirectToAction("Confirmation", new { id = model.BookingID });
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt phòng. Vui lòng thử lại sau.";
                    return RedirectToAction("Detail", "Room", new { id = model.RoomID });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception when booking: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi đặt phòng: {ex.Message}";
                return RedirectToAction("Detail", "Room", new { id = model.RoomID });
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

        [HttpGet]
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