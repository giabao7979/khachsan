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
            ViewBag.Rooms = new SelectList(await _context.Rooms.ToListAsync(), "RoomID", "RoomName");
            return PartialView("_Create", new Booking());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Rooms = new SelectList(await _context.Rooms.ToListAsync(), "RoomID", "RoomName");
            return PartialView("_Create", booking);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(Booking booking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Dữ liệu không hợp lệ!");
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

            if (booking.BookingID == 0)
            {
                _context.Bookings.Add(booking);
            }
            else
            {
                _context.Bookings.Update(booking);
            }

            await _context.SaveChangesAsync();
            return Ok();
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
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}