using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBK.Data;
using HotelBK.Models;
using System.Security.Claims;

namespace HotelBK.Controllers
{
    public class PaymentController : Controller
    {
        private readonly HotelContext _context;

        public PaymentController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int bookingId)
        {
            // Kiểm tra nếu booking tồn tại
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Kiểm tra xem booking có thuộc về người dùng hiện tại không
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (booking.Email != userEmail && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thanh toán đặt phòng này.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Tính tổng tiền
            var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
            if (totalDays < 1) totalDays = 1;
            var totalAmount = booking.Room.Price * totalDays * booking.RoomCount;

            // Tính số tiền đã thanh toán
            var paidAmount = await _context.Payments
                .Where(p => p.BookingID == bookingId && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            // Tính số tiền còn lại
            var remainingAmount = totalAmount - paidAmount;

            ViewBag.Booking = booking;
            ViewBag.TotalDays = totalDays;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.PaidAmount = paidAmount;
            ViewBag.RemainingAmount = remainingAmount;

            return View(new Payment { BookingID = bookingId, Amount = remainingAmount });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(Payment payment)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin thanh toán.";
                return RedirectToAction("Index", new { bookingId = payment.BookingID });
            }

            try
            {
                // Kiểm tra booking
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingID == payment.BookingID);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đặt phòng.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Tính tổng tiền
                var totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
                if (totalDays < 1) totalDays = 1;
                var totalAmount = booking.Room.Price * totalDays * booking.RoomCount;

                // Tính số tiền đã thanh toán
                var paidAmount = await _context.Payments
                    .Where(p => p.BookingID == payment.BookingID && p.Status == "Completed")
                    .SumAsync(p => p.Amount);

                // Tính số tiền còn lại
                var remainingAmount = totalAmount - paidAmount;

                // Kiểm tra số tiền thanh toán
                if (payment.Amount <= 0)
                {
                    TempData["ErrorMessage"] = "Số tiền thanh toán phải lớn hơn 0.";
                    return RedirectToAction("Index", new { bookingId = payment.BookingID });
                }

                if (payment.Amount > remainingAmount)
                {
                    TempData["ErrorMessage"] = $"Số tiền thanh toán không thể lớn hơn số tiền còn lại ({remainingAmount:N0} VNĐ).";
                    return RedirectToAction("Index", new { bookingId = payment.BookingID });
                }

                // Cập nhật thông tin thanh toán
                payment.PaymentDate = DateTime.Now;
                payment.Status = "Completed";

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Kiểm tra nếu đã thanh toán đủ
                if (paidAmount + payment.Amount >= totalAmount)
                {
                    booking.Status = "Paid";
                    _context.Bookings.Update(booking);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thanh toán thành công!";
                return RedirectToAction("PaymentSuccess", new { paymentId = payment.PaymentID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xử lý thanh toán: {ex.Message}";
                return RedirectToAction("Index", new { bookingId = payment.BookingID });
            }
        }

        public async Task<IActionResult> PaymentSuccess(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction("MyBookings", "Booking");
            }

            return View(payment);
        }

        public async Task<IActionResult> PaymentHistory()
        {
            // Lấy email của người dùng hiện tại
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            // Lấy danh sách thanh toán của người dùng
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .Where(p => p.Booking.Email == userEmail)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }
    }
}