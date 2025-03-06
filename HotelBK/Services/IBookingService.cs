using HotelBK.Data;
using HotelBK.Models;

namespace HotelBK.Services
{
    public interface IBookingService
    {
        Task<bool> KiemTraPhongTrong(int roomId, DateTime checkIn, DateTime checkOut);
        Task<decimal> TinhTienPhong(int roomId, DateTime checkIn, DateTime checkOut, int roomCount);
        Task<Booking> TaoDatPhong(Booking booking);
    }
}