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
            try
            {
                // Đảm bảo phòng tồn tại
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null)
                    return false;

                // Nếu phòng trong trạng thái "Bảo trì", luôn trả về false
                if (room.Status == "Bảo trì")
                    return false;

                // Nếu phòng không ở trạng thái "Còn trống" và không phải trường hợp cập nhật
                if (room.Status != "Còn trống" && excludeBookingId == null)
                    return false;

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

                // Ghi log để debug
                System.Diagnostics.Debug.WriteLine($"Room {roomId} availability check from {checkIn} to {checkOut}: {!existingBookings}, Room status: {room.Status}");

                return !existingBookings;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Error checking room availability: {ex.Message}");
                return false;
            }
        }

        public async Task<decimal> TinhTienPhong(int roomId, DateTime checkIn, DateTime checkOut, int roomCount)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null) return 0;

                int soNgay = (int)(checkOut - checkIn).TotalDays;
                if (soNgay <= 0) soNgay = 1; // Tối thiểu 1 ngày

                return room.Price * soNgay * roomCount;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Error calculating room price: {ex.Message}");
                return 0;
            }
        }

        public async Task<Booking> TaoDatPhong(Booking booking)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(booking.FullName) ||
                    string.IsNullOrEmpty(booking.Email) ||
                    string.IsNullOrEmpty(booking.Phone) ||
                    booking.RoomID <= 0 ||
                    booking.RoomCount <= 0)
                {
                    throw new ArgumentException("Dữ liệu đặt phòng không đầy đủ");
                }

                // Kiểm tra phòng có trống không
                bool isAvailable = await KiemTraPhongTrong(booking.RoomID, booking.CheckInDate, booking.CheckOutDate);
                if (!isAvailable)
                {
                    throw new InvalidOperationException("Phòng đã được đặt trong khoảng thời gian này");
                }

                // Tạo đặt phòng
                booking.Status = booking.Status ?? "Pending";
                booking.CreatedAt = DateTime.Now;

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return booking;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Error creating booking: {ex.Message}");
                return booking; // Trả về booking object với BookingID = 0
            }
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