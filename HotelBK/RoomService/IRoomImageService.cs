using HotelBK.Models;

namespace HotelBK.Services
{
    public interface IRoomImageService
    {
        Task<IEnumerable<RoomImage>> GetRoomImagesByRoomId(int roomId);
        Task<RoomImage> GetMainImageByRoomId(int roomId);
        Task<RoomImage> AddRoomImage(RoomImage roomImage, IFormFile imageFile);
        Task<bool> DeleteRoomImage(int imageId);
        Task<bool> SetAsMainImage(int imageId);
        Task<bool> UpdateDisplayOrder(int imageId, int newOrder);
    }
}