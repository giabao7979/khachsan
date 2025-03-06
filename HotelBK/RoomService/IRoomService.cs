using HotelBK.Models;

namespace HotelBK.Services
{
    public interface IRoomService
    {
        Task<IEnumerable<Room>> GetAllRooms();
        Task<IEnumerable<Room>> GetAvailableRooms(DateTime checkIn, DateTime checkOut);
        Task<Room> GetRoomById(int roomId);
        Task<Room> CreateRoom(Room room);
        Task<Room> UpdateRoom(Room room);
        Task<bool> DeleteRoom(int roomId);
    }
}
