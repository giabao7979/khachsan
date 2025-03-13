using Microsoft.AspNetCore.Mvc;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace HotelBK.Controllers
{
    public class TestController : Controller
    {
        private readonly HotelContext _context;

        public TestController(HotelContext context)
        {
            _context = context;
        }

        // Endpoint để kiểm tra kết nối đến database
        public IActionResult Index()
        {
            try
            {
                var roomCount = _context.Rooms.Count();
                var bookingCount = _context.Bookings.Count();

                return Content($"Kết nối database thành công. Số phòng: {roomCount}, Số đặt phòng: {bookingCount}");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi kết nối database: {ex.Message}");
            }
        }

        // Endpoint để kiểm tra cấu trúc bảng Booking
        public IActionResult CheckBookingStructure()
        {
            try
            {
                var properties = typeof(Booking).GetProperties();
                var result = "Cấu trúc Booking Model:\n";

                foreach (var prop in properties)
                {
                    result += $"- {prop.Name}: {prop.PropertyType.Name}\n";
                }

                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }

        // Endpoint để thử tạo một đơn đặt phòng đơn giản
        public async Task<IActionResult> TestCreateBooking()
        {
            try
            {
                // Lấy một phòng từ database để test
                var room = await _context.Rooms.FirstOrDefaultAsync();
                if (room == null)
                {
                    return Content("Không tìm thấy phòng nào để test. Hãy tạo phòng trước.");
                }

                var booking = new Booking
                {
                    FullName = "Khách Hàng Test",
                    Email = "test@example.com",
                    Phone = "0123456789",
                    CheckInDate = DateTime.Now.AddDays(1),
                    CheckOutDate = DateTime.Now.AddDays(2),
                    RoomID = room.RoomID,
                    RoomCount = 1,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    SpecialRequest = "Đây là yêu cầu test"
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return Content($"Tạo đặt phòng test thành công. ID: {booking.BookingID}");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi khi tạo đặt phòng: {ex.Message}\n\nStack trace: {ex.StackTrace}");
            }
        }

        // Endpoint để kiểm tra chi tiết một đặt phòng
        public async Task<IActionResult> ViewBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingID == id);

                if (booking == null)
                {
                    return Content($"Không tìm thấy đặt phòng với ID {id}");
                }

                var result = $"Thông tin đặt phòng #{booking.BookingID}:\n" +
                             $"- Tên: {booking.FullName}\n" +
                             $"- Email: {booking.Email}\n" +
                             $"- Phòng: {booking.Room.RoomName} (ID: {booking.RoomID})\n" +
                             $"- Check-in: {booking.CheckInDate:dd/MM/yyyy}\n" +
                             $"- Check-out: {booking.CheckOutDate:dd/MM/yyyy}\n" +
                             $"- Trạng thái: {booking.Status}\n" +
                             $"- Yêu cầu đặc biệt: {booking.SpecialRequest}";

                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"Lỗi khi xem đặt phòng: {ex.Message}");
            }
        }

        // Endpoint để test quy trình đặt phòng đầy đủ
        public async Task<IActionResult> FullBookingTest()
        {
            try
            {
                // 1. Lấy một phòng ngẫu nhiên
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.Status == "Còn trống");

                if (room == null)
                {
                    return Content("Không tìm thấy phòng trống nào để test.");
                }

                // 2. Tạo booking mới
                var booking = new Booking
                {
                    FullName = "Test Full Booking",
                    Email = "fulltest@example.com",
                    Phone = "0987654321",
                    CheckInDate = DateTime.Now.AddDays(5),
                    CheckOutDate = DateTime.Now.AddDays(7),
                    RoomID = room.RoomID,
                    RoomCount = 1,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    SpecialRequest = string.Empty // Test với trường rỗng
                };

                // 3. Thử lưu booking
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 4. Lấy booking vừa tạo để kiểm tra
                var savedBooking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingID == booking.BookingID);

                var result = "=== TEST ĐẶT PHÒNG THÀNH CÔNG ===\n\n" +
                             $"Thông tin phòng: {room.RoomName} - {room.RoomType?.TypeName}\n" +
                             $"Đặt phòng ID: {savedBooking.BookingID}\n" +
                             $"Tên khách: {savedBooking.FullName}\n" +
                             $"Ngày nhận: {savedBooking.CheckInDate:dd/MM/yyyy}\n" +
                             $"Ngày trả: {savedBooking.CheckOutDate:dd/MM/yyyy}\n" +
                             $"Trạng thái: {savedBooking.Status}";

                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"=== TEST ĐẶT PHÒNG THẤT BẠI ===\n\n" +
                               $"Lỗi: {ex.Message}\n\n" +
                               $"Chi tiết: {ex.InnerException?.Message}\n\n" +
                               $"Stack trace: {ex.StackTrace}");
            }
        }
        // Thêm vào TestController
        public async Task<IActionResult> ListAllData()
        {
            try
            {
                var rooms = await _context.Rooms.ToListAsync();
                var bookings = await _context.Bookings.ToListAsync();

                string result = "=== DANH SÁCH PHÒNG ===\n";
                foreach (var room in rooms)
                {
                    result += $"ID: {room.RoomID}, Tên: {room.RoomName}, Trạng thái: {room.Status}\n";
                }

                result += "\n=== DANH SÁCH ĐẶT PHÒNG ===\n";
                foreach (var booking in bookings)
                {
                    result += $"ID: {booking.BookingID}, Tên KH: {booking.FullName}, Phòng ID: {booking.RoomID}, Check-in: {booking.CheckInDate:dd/MM/yyyy}\n";
                }

                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }
    }
}