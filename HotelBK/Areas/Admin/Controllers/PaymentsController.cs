using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class PaymentsController : Controller
    {
        private readonly HotelContext _context;

        public PaymentsController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        public async Task<IActionResult> Create()
        {
            // Lấy danh sách các booking chưa thanh toán hoặc thanh toán một phần
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.Status != "Cancelled" &&
                      (b.Status == "Confirmed" || b.Status == "Checked-in" || b.Status == "Checked-out"))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Kiểm tra booking nào đã được thanh toán đầy đủ
            var paidBookings = await _context.Payments
                .GroupBy(p => p.BookingID)
                .Select(g => new { BookingId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
                .ToListAsync();

            // Tạo danh sách booking chưa thanh toán đầy đủ
            var unpaidBookings = new List<SelectListItem>();

            foreach (var booking in bookings)
            {
                // Tính tổng tiền booking
                var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
                if (totalDays < 1) totalDays = 1;
                var totalAmount = booking.Room.Price * totalDays * booking.RoomCount;

                // Tìm số tiền đã thanh toán
                var paid = paidBookings.FirstOrDefault(p => p.BookingId == booking.BookingID);
                var paidAmount = paid != null ? paid.TotalPaid : 0;

                // Kiểm tra nếu chưa thanh toán hết
                if (paidAmount < totalAmount)
                {
                    unpaidBookings.Add(new SelectListItem
                    {
                        Value = booking.BookingID.ToString(),
                        Text = $"{booking.FullName} - {booking.Room.RoomName} - {(totalAmount - paidAmount).ToString("N0")} VNĐ còn lại"
                    });
                }
            }

            ViewBag.Bookings = unpaidBookings;
            return PartialView("_Create", new Payment { PaymentDate = DateTime.Now });
        }

        public async Task<IActionResult> View(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            return PartialView("_View", payment);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Payment payment)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu không hợp lệ");
                }

                if (payment.Amount <= 0)
                {
                    return BadRequest("Số tiền thanh toán phải lớn hơn 0");
                }

                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingID == payment.BookingID);

                if (booking == null)
                {
                    return BadRequest("Không tìm thấy thông tin đặt phòng");
                }

                // Tính tổng tiền booking
                var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
                if (totalDays < 1) totalDays = 1;
                var totalAmount = booking.Room.Price * totalDays * booking.RoomCount;

                // Tính số tiền đã thanh toán
                var paidAmount = await _context.Payments
                    .Where(p => p.BookingID == booking.BookingID)
                    .SumAsync(p => p.Amount);

                // Kiểm tra số tiền thanh toán có hợp lệ không
                if (payment.Amount > (totalAmount - paidAmount))
                {
                    return BadRequest($"Số tiền thanh toán vượt quá số tiền còn lại ({(totalAmount - paidAmount).ToString("N0")} VNĐ)");
                }

                payment.PaymentDate = DateTime.Now;
                payment.Status = "Completed";

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Kiểm tra xem đã thanh toán đủ chưa và cập nhật trạng thái booking nếu cần
                if ((paidAmount + payment.Amount) >= totalAmount)
                {
                    booking.Status = "Paid";
                    _context.Bookings.Update(booking);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Thanh toán thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return NotFound();
                }

                // Không cho phép xóa thanh toán đã hoàn thành
                if (payment.Status == "Completed" && DateTime.Now.Subtract(payment.PaymentDate).TotalDays > 1)
                {
                    return BadRequest("Không thể xóa thanh toán đã hoàn thành quá 24 giờ");
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                // Cập nhật lại trạng thái booking nếu cần
                var booking = await _context.Bookings.FindAsync(payment.BookingID);
                if (booking != null && booking.Status == "Paid")
                {
                    // Kiểm tra xem còn thanh toán nào không
                    var remainingPayments = await _context.Payments
                        .Where(p => p.BookingID == booking.BookingID)
                        .SumAsync(p => p.Amount);

                    // Nếu không còn thanh toán nào, đổi trạng thái về Confirmed
                    if (remainingPayments == 0)
                    {
                        booking.Status = "Confirmed";
                        _context.Bookings.Update(booking);
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi: " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Tính tổng tiền booking
            var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
            if (totalDays < 1) totalDays = 1;
            var totalAmount = booking.Room.Price * totalDays * booking.RoomCount;

            // Tính số tiền đã thanh toán
            var paidAmount = await _context.Payments
                .Where(p => p.BookingID == booking.BookingID)
                .SumAsync(p => p.Amount);

            // Tính số tiền còn lại
            var remainingAmount = totalAmount - paidAmount;

            return Json(new
            {
                bookingId = booking.BookingID,
                customerName = booking.FullName,
                roomName = booking.Room.RoomName,
                totalAmount = totalAmount,
                paidAmount = paidAmount,
                remainingAmount = remainingAmount,
                checkInDate = booking.CheckInDate.ToString("dd/MM/yyyy"),
                checkOutDate = booking.CheckOutDate.ToString("dd/MM/yyyy"),
                status = booking.Status
            });
        }
    }
}