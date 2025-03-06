using HotelBK.Data;
using Microsoft.AspNetCore.Mvc;

namespace HotelBK.Controllers
{
    public class TestDbController : Controller
    {
        private readonly HotelContext _context;

        public TestDbController(HotelContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            try
            {
                // Kiểm tra kết nối bằng cách lấy danh sách phòng
                var rooms = _context.Rooms.ToList();
                return Ok("Kết nối thành công! Số lượng phòng: " + rooms.Count);
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi kết nối: " + ex.Message);
            }
        }
    }
}
