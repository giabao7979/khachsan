using HotelBK.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelBK.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetApprovedReviewsByRoomId(int roomId);
        Task<Review> AddReview(Review review);
        Task<bool> ApproveReview(int reviewId);
        Task<bool> DeleteReview(int reviewId);
    }
}