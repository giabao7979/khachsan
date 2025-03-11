using HotelBK.Data;
using HotelBK.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public class BookingController : Controller
{
    private readonly HotelContext _context;

    public BookingController(HotelContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? roomId, DateTime? checkIn, DateTime? checkOut, int? adults, int? children, int? roomCount)
    {
        if (roomId.HasValue)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomID == roomId.Value);

            if (room != null)
            {
                ViewBag.Room = room;
                ViewBag.CheckIn = checkIn ?? DateTime.Now.AddDays(1);
                ViewBag.CheckOut = checkOut ?? DateTime.Now.AddDays(2);
                ViewBag.Adults = adults ?? 2;
                ViewBag.Children = children ?? 0;
                ViewBag.RoomCount = roomCount ?? 1;
            }
        }

        return View();
    }

    [HttpPost]
    [Authorize] // Yêu cầu đăng nhập
    public async Task<IActionResult> BookRoom(Booking model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        // Kiểm tra xem phòng có trống không
        var isRoomAvailable = await _context.Bookings
            .Where(b => b.RoomID == model.RoomID &&
                     b.Status != "Cancelled" &&
                     ((model.CheckInDate >= b.CheckInDate && model.CheckInDate < b.CheckOutDate) ||
                      (model.CheckOutDate > b.CheckInDate && model.CheckOutDate <= b.CheckOutDate) ||
                      (model.CheckInDate <= b.CheckInDate && model.CheckOutDate >= b.CheckOutDate)))
            .AnyAsync();

        if (isRoomAvailable)
        {
            ModelState.AddModelError("", "Phòng này đã được đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác hoặc phòng khác.");
            return View("Index", model);
        }

        // Tạo đơn đặt phòng mới
        model.Status = "Pending";
        model.CreatedAt = DateTime.Now;

        // Kiểm tra nếu người dùng đã đăng nhập, lấy thông tin từ tài khoản
        if (User.Identity.IsAuthenticated)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                model.FullName = user.FullName;
                model.Email = user.Email;
                model.Phone = user.Phone;
            }
        }

        _context.Bookings.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đặt phòng thành công! Mã đặt phòng của bạn là: " + model.BookingID;
        return RedirectToAction("Confirmation", new { id = model.BookingID });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BookingID == id);

        if (booking == null)
        {
            return NotFound();
        }

        return View(booking);
    }

}