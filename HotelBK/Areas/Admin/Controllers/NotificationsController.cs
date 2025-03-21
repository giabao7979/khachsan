using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class NotificationsController : Controller
    {
        private readonly HotelContext _context;

        public NotificationsController(HotelContext context)
        {
            _context = context;
        }

        // API lấy thông báo đặt phòng mới
        [HttpGet]
        public async Task<IActionResult> GetBookingNotifications()
        {
            // Lấy 5 đặt phòng mới nhất chưa đọc
            var newBookings = await _context.Bookings
                .Where(b => !b.IsRead)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new
                {
                    b.BookingID,
                    b.FullName,
                    TimeAgo = GetTimeAgo(b.CreatedAt),
                    b.CreatedAt
                })
                .ToListAsync();

            // Số lượng tất cả các đặt phòng chưa đọc
            var count = await _context.Bookings.CountAsync(b => !b.IsRead);

            return Json(new { items = newBookings, count });
        }

        // API lấy thông báo tin nhắn liên hệ mới
        [HttpGet]
        public async Task<IActionResult> GetContactNotifications()
        {
            // Lấy 5 tin nhắn liên hệ mới nhất chưa đọc
            var newMessages = await _context.ContactMessages
                .Where(c => !c.IsRead)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new
                {
                    c.ContactID,
                    c.Name,
                    c.Subject,
                    TimeAgo = GetTimeAgo(c.CreatedAt),
                    c.CreatedAt
                })
                .ToListAsync();

            // Số lượng tất cả các tin nhắn chưa đọc
            var count = await _context.ContactMessages.CountAsync(c => !c.IsRead);

            return Json(new { items = newMessages, count });
        }

        // API đánh dấu đã đọc tất cả thông báo đặt phòng
        [HttpPost]
        public async Task<IActionResult> MarkAllBookingsAsRead()
        {
            var unreadBookings = await _context.Bookings
                .Where(b => !b.IsRead)
                .ToListAsync();

            foreach (var booking in unreadBookings)
            {
                booking.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // API đánh dấu đã đọc tất cả tin nhắn liên hệ
        [HttpPost]
        public async Task<IActionResult> MarkAllContactsAsRead()
        {
            var unreadMessages = await _context.ContactMessages
                .Where(c => !c.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // API đánh dấu một thông báo đặt phòng đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkBookingAsRead(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // API đánh dấu một tin nhắn liên hệ đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkContactAsRead(int id)
        {
            var contact = await _context.ContactMessages.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            contact.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // Phương thức tính thời gian tương đối (ví dụ: "2 phút trước")
        private string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;

            if (span.TotalDays > 365)
            {
                int years = (int)(span.TotalDays / 365);
                return $"{years} năm trước";
            }
            if (span.TotalDays > 30)
            {
                int months = (int)(span.TotalDays / 30);
                return $"{months} tháng trước";
            }
            if (span.TotalDays > 1)
            {
                return $"{(int)span.TotalDays} ngày trước";
            }
            if (span.TotalHours > 1)
            {
                return $"{(int)span.TotalHours} giờ trước";
            }
            if (span.TotalMinutes > 1)
            {
                return $"{(int)span.TotalMinutes} phút trước";
            }
            return "Vừa xong";
        }
    }
}