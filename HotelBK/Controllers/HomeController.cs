using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBK.Data;
using HotelBK.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelBK.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelContext _context;

        public HomeController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy 3 phòng nổi bật (có lượt xem cao nhất)
                var featuredRooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .OrderByDescending(r => r.ViewCount)
                    .Take(3)
                    .ToListAsync();

                // Lấy các đánh giá gần đây
                var recentReviews = await _context.Reviews
                    .Include(r => r.Room)
                    .Where(r => r.IsApproved)
                    .OrderByDescending(r => r.ReviewDate)
                    .Take(3)
                    .ToListAsync();

                // Đếm số liệu thống kê từ cơ sở dữ liệu
                var roomsCount = await _context.Rooms.CountAsync();
                var staffCount = await _context.Users.Where(u => u.RoleID == 3).CountAsync(); // Vai trò 3 là Staff
                var customersCount = await _context.Bookings.Select(b => b.Email).Distinct().CountAsync(); // Số khách hàng duy nhất đặt phòng

                // Truyền dữ liệu vào ViewBag
                ViewBag.FeaturedRooms = featuredRooms;
                ViewBag.RecentReviews = recentReviews;
                ViewBag.RoomsCount = roomsCount;
                ViewBag.StaffCount = staffCount;
                ViewBag.CustomersCount = customersCount;

                return View();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Lỗi trong HomeController.Index: {ex.Message}");

                // Trả về view với các danh sách trống trong trường hợp lỗi
                ViewBag.FeaturedRooms = new List<Room>();
                ViewBag.RecentReviews = new List<Review>();
                ViewBag.RoomsCount = 0;
                ViewBag.StaffCount = 0;
                ViewBag.CustomersCount = 0;
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại sau.";

                return View();
            }
        }

        public IActionResult Booking()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}