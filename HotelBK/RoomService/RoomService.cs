using HotelBK.Data;
using HotelBK.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Services
{
    public class RoomService : IRoomService
    {
        private readonly HotelContext _context;

        public RoomService(HotelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Room>> GetAllRooms()
        {
            return await _context.Rooms
                .Include(r => r.RoomType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            var bookedRoomIds = await _context.Bookings
                .Where(b => b.Status != "Cancelled" &&
                         ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                          (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                          (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)))
                .Select(b => b.RoomID)
                .ToListAsync();

            return await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => !bookedRoomIds.Contains(r.RoomID))
                .ToListAsync();
        }

        public async Task<Room> GetRoomById(int roomId)
        {
            return await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomID == roomId);
        }

        public async Task<Room> CreateRoom(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<Room> UpdateRoom(Room room)
        {
            _context.Entry(room).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<bool> DeleteRoom(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}