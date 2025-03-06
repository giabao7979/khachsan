using HotelBK.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Controllers
{
    public class TestDbController : Controller
    {
        private readonly HotelContext _context;

        public TestDbController(HotelContext context)
        {
            _context = context;
        }

        // GET: /TestDb
        public IActionResult Index()
        {
            try
            {
                // Kiểm tra kết nối đến database
                _context.Database.OpenConnection();
                _context.Database.CloseConnection();

                // Thông tin chi tiết về database
                var databaseInfo = new
                {
                    ConnectionString = _context.Database.GetConnectionString(),
                    ProviderName = _context.Database.ProviderName,
                    DbSetNames = string.Join(", ", _context.GetType().GetProperties()
                        .Where(p => p.PropertyType.Name.Contains("DbSet"))
                        .Select(p => p.Name))
                };

                // Thử lấy danh sách bảng từ database
                var tableNames = new List<string>();
                try
                {
                    tableNames = _context.Database.SqlQuery<string>($"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'").ToList();
                }
                catch (Exception ex)
                {
                    ViewBag.TableError = ex.Message;
                }

                // Thử truy vấn trực tiếp đến bảng RoomTypes
                var roomTypesData = new List<object>();
                try
                {
                    roomTypesData = _context.Database.SqlQuery<object>($"SELECT RoomTypeID, TypeName, Description FROM RoomTypes").ToList();
                }
                catch (Exception ex)
                {
                    ViewBag.RoomTypesError = ex.Message;
                }

                // Thử truy vấn qua Entity Framework
                var roomTypes = new List<dynamic>();
                try
                {
                    roomTypes = _context.Set<dynamic>("RoomTypes").ToList();
                }
                catch (Exception ex)
                {
                    ViewBag.EFError = ex.Message;
                }

                // Truyền dữ liệu cho view
                ViewBag.DatabaseInfo = databaseInfo;
                ViewBag.TableNames = tableNames;
                ViewBag.RoomTypesData = roomTypesData;
                ViewBag.RoomTypes = roomTypes;

                return View();
            }
            catch (Exception ex)
            {
                return Content($"Lỗi kết nối: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }

        // GET: /TestDb/DirectQuery
        public IActionResult DirectQuery()
        {
            try
            {
                var result = new List<object>();

                // Thử truy vấn SQL trực tiếp
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT * FROM RoomTypes";

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        command.Connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            result.Add(row);
                        }
                    }
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return Content($"Lỗi truy vấn trực tiếp: {ex.Message}");
            }
        }

        // GET: /TestDb/CheckModel
        public IActionResult CheckModel()
        {
            var modelInfo = new List<string>();
            foreach (var entityType in _context.Model.GetEntityTypes())
            {
                modelInfo.Add($"Entity: {entityType.Name}");
                modelInfo.Add($"Table: {entityType.GetTableName()}");
                modelInfo.Add($"Schema: {entityType.GetSchema() ?? "dbo"}");
                modelInfo.Add("-----------");
            }

            return Content(string.Join("\n", modelInfo));
        }
    }
}