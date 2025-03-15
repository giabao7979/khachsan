using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class AdminHomeController : Controller
    {
        private readonly HotelContext _context;

        public AdminHomeController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê tổng số phòng
            var totalRooms = await _context.Rooms.CountAsync();
            ViewBag.TotalRooms = totalRooms;

            // Thống kê tổng số người dùng
            var totalUsers = await _context.Users.CountAsync();
            ViewBag.TotalUsers = totalUsers;

            // Thống kê tổng số đặt phòng
            var totalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalBookings = totalBookings;

            // Thống kê doanh thu
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
            ViewBag.TotalRevenue = totalRevenue;

            // Đặt phòng gần đây
            var recentBookings = await _context.Bookings
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentBookings = recentBookings;

            // Phòng được đặt nhiều nhất
            var topRooms = await _context.Bookings
                .GroupBy(b => b.RoomID)
                .Select(g => new
                {
                    RoomID = g.Key,
                    BookingCount = g.Count()
                })
                .OrderByDescending(x => x.BookingCount)
                .Take(5)
                .ToListAsync();

            var topRoomDetails = new List<(Room Room, int BookingCount)>();
            foreach (var item in topRooms)
            {
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomID == item.RoomID);
                if (room != null)
                {
                    topRoomDetails.Add((Room: room, BookingCount: item.BookingCount));
                }
            }
            ViewBag.TopRooms = topRoomDetails;

            // Doanh thu theo tháng trong năm hiện tại
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" && p.PaymentDate.Year == currentYear)
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            var revenueData = new decimal[12];
            foreach (var item in monthlyRevenue)
            {
                revenueData[item.Month - 1] = item.Revenue;
            }
            ViewBag.MonthlyRevenue = revenueData;

            // Thống kê đặt phòng theo trạng thái
            var bookingStatusStats = await _context.Bookings
                .GroupBy(b => b.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            ViewBag.BookingStatusStats = bookingStatusStats;

            return View();
        }
    }
}