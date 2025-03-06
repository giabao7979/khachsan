using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Controllers
{
    public class DebugController : Controller
    {
        private readonly HotelContext _context;

        public DebugController(HotelContext context)
        {
            _context = context;
        }

        // GET: /Debug
        public IActionResult Index()
        {
            try
            {
                // Kiểm tra kết nối và liệt kê các bảng
                var tables = new List<string>();
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        command.Connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }

                ViewBag.Tables = tables;

                // Cố gắng truy vấn RoomTypes trực tiếp bằng SQL
                var roomTypes = new List<dynamic>();
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT * FROM RoomTypes";

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        command.Connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new
                            {
                                RoomTypeID = reader.GetInt32(0),
                                TypeName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                            };
                            roomTypes.Add(item);
                        }
                    }
                }

                ViewBag.RoomTypesData = roomTypes;

                return View();
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }
}