using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelBK.Areas.Admin.Filters;
using HotelBK.Services;
using HotelBK.Data;
using HotelBK.Models;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class BookingsController : Controller
    {
        private readonly HotelContext _context;
        private readonly IBookingService _bookingService;

        public BookingsController(HotelContext context, IBookingService bookingService)
        {
            _context = context;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Rooms = new SelectList(await _context.Rooms
                .Where(r => r.Status == "Còn trống")
                .ToListAsync(), "RoomID", "RoomName");
            return PartialView("_Create", new Booking());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Rooms = new SelectList(await _context.Rooms.ToListAsync(), "RoomID", "RoomName", booking.RoomID);
            return PartialView("_Create", booking);
        }

        public async Task<IActionResult> View(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Tính tổng số tiền
            var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
            if (totalDays < 1) totalDays = 1;

            ViewBag.TotalDays = totalDays;
            ViewBag.TotalAmount = totalDays * booking.Room.Price * booking.RoomCount;

            return PartialView("_View", booking);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Booking booking)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest("Dữ liệu không hợp lệ: " + errors);
                }

                // Kiểm tra ngày đặt phòng hợp lệ
                if (booking.CheckInDate >= booking.CheckOutDate)
                {
                    return BadRequest("Ngày nhận phòng phải trước ngày trả phòng!");
                }

                if (booking.CheckInDate.Date < DateTime.Now.Date)
                {
                    return BadRequest("Ngày nhận phòng không thể trong quá khứ!");
                }

                // Kiểm tra phòng có trống không
                if (booking.BookingID == 0) // Trường hợp tạo mới
                {
                    var isRoomAvailable = await _bookingService.KiemTraPhongTrong(
                        booking.RoomID, booking.CheckInDate, booking.CheckOutDate);

                    if (!isRoomAvailable)
                    {
                        return BadRequest("Phòng đã được đặt trong khoảng thời gian này!");
                    }

                    booking.CreatedAt = DateTime.Now;
                }
                else
                {
                    // Trường hợp cập nhật
                    var originalBooking = await _context.Bookings.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.BookingID == booking.BookingID);

                    if (originalBooking == null)
                    {
                        return BadRequest("Không tìm thấy đơn đặt phòng!");
                    }

                    // Nếu ngày hoặc phòng thay đổi, cần kiểm tra lại
                    if (originalBooking.RoomID != booking.RoomID ||
                        originalBooking.CheckInDate != booking.CheckInDate ||
                        originalBooking.CheckOutDate != booking.CheckOutDate)
                    {
                        var isRoomAvailable = await _bookingService.KiemTraPhongTrong(
                            booking.RoomID, booking.CheckInDate, booking.CheckOutDate, booking.BookingID);

                        if (!isRoomAvailable)
                        {
                            return BadRequest("Phòng đã được đặt trong khoảng thời gian này!");
                        }
                    }
                }

                if (booking.BookingID == 0)
                {
                    _context.Bookings.Add(booking);
                }
                else
                {
                    _context.Bookings.Update(booking);
                }

                await _context.SaveChangesAsync();

                // Cập nhật trạng thái phòng nếu booking ở trạng thái "Checked-in"
                if (booking.Status == "Checked-in")
                {
                    var room = await _context.Rooms.FindAsync(booking.RoomID);
                    if (room != null)
                    {
                        room.Status = "Đang ở";
                        _context.Rooms.Update(room);
                        await _context.SaveChangesAsync();
                    }
                }

                // Cập nhật trạng thái phòng về "Còn trống" nếu booking ở trạng thái "Checked-out" hoặc "Cancelled"
                if (booking.Status == "Checked-out" || booking.Status == "Cancelled")
                {
                    var room = await _context.Rooms.FindAsync(booking.RoomID);
                    if (room != null)
                    {
                        // Kiểm tra nếu không có booking nào khác đang sử dụng phòng này
                        var activeBookings = await _context.Bookings.CountAsync(b =>
                            b.RoomID == booking.RoomID &&
                            b.Status == "Checked-in" &&
                            b.BookingID != booking.BookingID);

                        if (activeBookings == 0)
                        {
                            room.Status = "Còn trống";
                            _context.Rooms.Update(room);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return Ok(new { success = true, bookingId = booking.BookingID });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = status;
            _context.Bookings.Update(booking);

            // Cập nhật trạng thái phòng tương ứng
            var room = await _context.Rooms.FindAsync(booking.RoomID);
            if (room != null)
            {
                if (status == "Checked-in")
                {
                    room.Status = "Đang ở";
                }
                else if (status == "Checked-out" || status == "Cancelled")
                {
                    // Kiểm tra xem có booking nào khác đang sử dụng phòng này không
                    var activeBookings = await _context.Bookings.CountAsync(b =>
                        b.RoomID == booking.RoomID &&
                        b.Status == "Checked-in" &&
                        b.BookingID != booking.BookingID);

                    if (activeBookings == 0)
                    {
                        room.Status = "Còn trống";
                    }
                }

                _context.Rooms.Update(room);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}