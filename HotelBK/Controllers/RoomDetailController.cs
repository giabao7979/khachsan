using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBK.Data;
using HotelBK.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelBK.Services; 

namespace HotelBK.Controllers
{
    public class RoomDetailController : Controller
    {
        private readonly HotelContext _context;
        private readonly IReviewService _reviewService;

        public RoomDetailController(HotelContext context, IReviewService reviewService)
        {
            _context = context;
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Room");
            }

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }
            // Tăng số lượt xem phòng
            room.ViewCount++;
            _context.Update(room);
            await _context.SaveChangesAsync();

            var reviews = await _context.Reviews
                .Where(r => r.RoomID == id)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();
            ViewBag.Reviews = reviews;

            // Lấy danh sách phòng liên quan (không bao gồm phòng hiện tại)
            var relatedRooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.RoomID != id) // Loại trừ phòng hiện tại
                .OrderBy(r => Guid.NewGuid()) // Lấy ngẫu nhiên
                .Take(6) // Lấy 6 phòng để hiển thị 3 phòng mỗi slide
                .ToListAsync();

            // Truyền dữ liệu sang view
            ViewBag.RelatedRooms = relatedRooms;

            // Sắp xếp ảnh theo thứ tự, và đảm bảo ảnh chính hiển thị đầu tiên
            room.RoomImages = room.RoomImages
                .OrderByDescending(img => img.IsMainImage)
                .ThenBy(img => img.DisplayOrder)
                .ToList();

            return View(room);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(int RoomID, string Name, string Email, string Comment)
        {
            try
            {
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Comment))
                {
                    return Json(new { success = false, message = "Tên và nội dung đánh giá là bắt buộc" });
                }

                var review = new Review
                {
                    RoomID = RoomID,
                    Name = Name,
                    Email = Email,
                    Comment = Comment,
                    ReviewDate = DateTime.Now,
                    IsApproved = true
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}