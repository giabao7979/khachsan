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

        public async Task<bool> KiemTraPhongTrong(int roomId, DateTime checkIn, DateTime checkOut)
        {
            // Kiểm tra xem phòng có trống trong khoảng thời gian này không
            var existingBookings = await _context.Bookings
                .Where(b => b.RoomID == roomId &&
                         b.Status != "Cancelled" &&
                         ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                          (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                          (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)))
                .AnyAsync();

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
    }
}