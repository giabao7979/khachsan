using HotelBK.Data;
using HotelBK.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Services
{
    public class BookingServices : IBookingService
    {
        private readonly HotelContext _context;

        public BookingServices(HotelContext context)
        {
            _context = context;
        }

        public async Task<bool> KiemTraPhongTrong(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
        {
            // Kiểm tra xem phòng có trống trong khoảng thời gian này không
            var query = _context.Bookings
                .Where(b => b.RoomID == roomId &&
                         b.Status != "Cancelled" &&
                         ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                          (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                          (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)));

            // Nếu đang sửa booking, loại trừ booking hiện tại
            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.BookingID != excludeBookingId.Value);
            }

            var existingBookings = await query.AnyAsync();

            return !existingBookings;
        }

        public async Task<decimal> TinhTienPhong(int roomId, DateTime checkIn, DateTime checkOut, int roomCount)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return 0;

            int soNgay = (int)(checkOut - checkIn).TotalDays;
            if (soNgay <= 0) soNgay = 1; // Tối thiểu 1 ngày

            return room.Price * soNgay * roomCount;
        }

        public async Task<Booking> TaoDatPhong(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<List<Booking>> GetAllBookings()
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsByRoom(int roomId)
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.RoomID == roomId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsByStatus(string status)
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.Status == status)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking> GetBookingById(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);
        }
    }
}