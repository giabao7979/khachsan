using HotelBK.Data;
using HotelBK.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBK.Services
{
    public class ReviewService : IReviewService
    {
        private readonly HotelContext _context;

        public ReviewService(HotelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetApprovedReviewsByRoomId(int roomId)
        {
            return await _context.Reviews
                .Where(r => r.RoomID == roomId && r.IsApproved)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();
        }

        public async Task<Review> AddReview(Review review)
        {
            review.ReviewDate = DateTime.Now;
            review.IsApproved = true; // Mặc định đánh giá chưa được phê duyệt

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<bool> ApproveReview(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return false;

            review.IsApproved = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReview(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}