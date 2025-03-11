using HotelBK.Models;

namespace HotelBK.Services
{
    public interface IBookingService
    {
        Task<bool> KiemTraPhongTrong(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
        Task<decimal> TinhTienPhong(int roomId, DateTime checkIn, DateTime checkOut, int roomCount);
        Task<Booking> TaoDatPhong(Booking booking);
        Task<List<Booking>> GetAllBookings();
        Task<List<Booking>> GetBookingsByRoom(int roomId);
        Task<List<Booking>> GetBookingsByStatus(string status);
        Task<Booking> GetBookingById(int bookingId);
    }
}