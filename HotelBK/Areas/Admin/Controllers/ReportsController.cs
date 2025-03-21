using HotelBK.Areas.Admin.Filters;
using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;
using System.Data;
using System.Drawing;

namespace HotelBK.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAreaAuthorization]
    public class ReportsController : Controller
    {
        private readonly HotelContext _context;

        public ReportsController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Revenue(int? year, int? month)
        {
            // Mặc định là năm hiện tại nếu không có tham số
            int selectedYear = year ?? DateTime.Now.Year;

            // Lấy danh sách các năm có dữ liệu để hiển thị dropdown
            var years = await _context.Payments
                .Where(p => p.Status == "Completed")
                .Select(p => p.PaymentDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Nếu không có dữ liệu, thêm năm hiện tại
            if (!years.Any())
            {
                years.Add(DateTime.Now.Year);
            }
            else if (!years.Contains(selectedYear))
            {
                // Nếu năm được chọn không có trong danh sách, thêm vào
                years.Add(selectedYear);
                years = years.OrderByDescending(y => y).ToList();
            }

            ViewBag.Years = years;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = month;

            // Nếu tháng được chỉ định, hiển thị báo cáo chi tiết cho tháng đó
            if (month.HasValue)
            {
                return await GetMonthlyRevenueDetail(selectedYear, month.Value);
            }

            // Lấy doanh thu theo tháng trong năm được chọn
            var monthlyRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" && p.PaymentDate.Year == selectedYear)
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    BookingsCount = g.Select(p => p.BookingID).Distinct().Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho biểu đồ và bảng
            var revenueData = new decimal[12];
            var bookingsData = new int[12];
            var monthNames = new string[12]
            {
                "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
                "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"
            };

            // Điền dữ liệu
            foreach (var item in monthlyRevenue)
            {
                revenueData[item.Month - 1] = item.Revenue;
                bookingsData[item.Month - 1] = item.BookingsCount;
            }

            // Tính tổng doanh thu
            decimal totalRevenue = revenueData.Sum();

            // Tính doanh thu trung bình mỗi tháng (chỉ tính các tháng có doanh thu)
            decimal avgRevenue = 0;
            int monthsWithRevenue = revenueData.Count(r => r > 0);
            if (monthsWithRevenue > 0)
            {
                avgRevenue = totalRevenue / monthsWithRevenue;
            }

            // Truyền dữ liệu sang view
            ViewBag.RevenueData = revenueData;
            ViewBag.BookingsData = bookingsData;
            ViewBag.MonthNames = monthNames;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AvgRevenue = avgRevenue;

            return View();
        }

        private async Task<IActionResult> GetMonthlyRevenueDetail(int year, int month)
        {
            // Lấy doanh thu chi tiết theo ngày trong tháng
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var dailyRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" &&
                       p.PaymentDate >= startDate &&
                       p.PaymentDate <= endDate)
                .GroupBy(p => p.PaymentDate.Day)
                .Select(g => new
                {
                    Day = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    BookingsCount = g.Select(p => p.BookingID).Distinct().Count()
                })
                .OrderBy(x => x.Day)
                .ToListAsync();

            // Lấy chi tiết từng thanh toán trong tháng
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .Where(p => p.Status == "Completed" &&
                       p.PaymentDate >= startDate &&
                       p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Lấy chi tiết doanh thu theo loại phòng
            var revenueByRoomType = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(p => p.Status == "Completed" &&
                       p.PaymentDate >= startDate &&
                       p.PaymentDate <= endDate)
                .GroupBy(p => p.Booking.Room.RoomType.TypeName)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    BookingsCount = g.Select(p => p.BookingID).Distinct().Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            // Doanh thu theo phương thức thanh toán
            var revenueByPaymentMethod = await _context.Payments
                .Where(p => p.Status == "Completed" &&
                       p.PaymentDate >= startDate &&
                       p.PaymentDate <= endDate)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho biểu đồ
            int daysInMonth = DateTime.DaysInMonth(year, month);
            var dayLabels = new string[daysInMonth];
            var dayRevenueData = new decimal[daysInMonth];

            for (int i = 0; i < daysInMonth; i++)
            {
                dayLabels[i] = (i + 1).ToString();
                dayRevenueData[i] = 0;
            }

            foreach (var item in dailyRevenue)
            {
                dayRevenueData[item.Day - 1] = item.Revenue;
            }

            // Tính tổng doanh thu tháng
            decimal totalMonthlyRevenue = dayRevenueData.Sum();

            // Sử dụng tên tháng tiếng Việt
            string monthName = $"Tháng {month}";

            ViewBag.MonthName = monthName;
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.DayLabels = dayLabels;
            ViewBag.DayRevenueData = dayRevenueData;
            ViewBag.TotalMonthlyRevenue = totalMonthlyRevenue;
            ViewBag.Payments = payments;
            ViewBag.RevenueByRoomType = revenueByRoomType;
            ViewBag.RevenueByPaymentMethod = revenueByPaymentMethod;

            return View("MonthlyRevenueDetail");
        }

        [HttpGet]
        public async Task<IActionResult> ExportYearlyRevenueExcel(int year)
        {
            // Lấy dữ liệu doanh thu theo tháng
            var monthlyRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" && p.PaymentDate.Year == year)
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    BookingsCount = g.Select(p => p.BookingID).Distinct().Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Tạo workbook và worksheet
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Doanh thu theo tháng");

                // Tạo tiêu đề
                worksheet.Cell(1, 1).Value = $"BÁO CÁO DOANH THU NĂM {year}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 4).Merge();
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Tạo header
                worksheet.Cell(3, 1).Value = "Tháng";
                worksheet.Cell(3, 2).Value = "Doanh thu (VNĐ)";
                worksheet.Cell(3, 3).Value = "Số lượng đặt phòng";
                worksheet.Cell(3, 4).Value = "Doanh thu trung bình/đặt phòng (VNĐ)";

                worksheet.Range(3, 1, 3, 4).Style.Font.Bold = true;
                worksheet.Range(3, 1, 3, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

                // Điền dữ liệu
                int row = 4;
                decimal totalRevenue = 0;
                int totalBookings = 0;

                for (int month = 1; month <= 12; month++)
                {
                    string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                    var data = monthlyRevenue.FirstOrDefault(m => m.Month == month);
                    decimal revenue = data?.Revenue ?? 0;
                    int bookings = data?.BookingsCount ?? 0;
                    decimal avgRevenue = bookings > 0 ? revenue / bookings : 0;

                    totalRevenue += revenue;
                    totalBookings += bookings;

                    worksheet.Cell(row, 1).Value = monthName;
                    worksheet.Cell(row, 2).Value = (double)revenue;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(row, 3).Value = bookings;
                    worksheet.Cell(row, 4).Value = (double)avgRevenue;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                    row++;
                }

                // Tính tổng
                decimal totalAvgRevenue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

                // Thêm dòng tổng
                worksheet.Cell(row, 1).Value = "Tổng";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 2).Value = (double)totalRevenue;
                worksheet.Cell(row, 2).Style.Font.Bold = true;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 3).Value = totalBookings;
                worksheet.Cell(row, 3).Style.Font.Bold = true;
                worksheet.Cell(row, 4).Value = (double)totalAvgRevenue;
                worksheet.Cell(row, 4).Style.Font.Bold = true;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                // Căn chỉnh cột
                worksheet.Columns().AdjustToContents();

                // Thêm đường viền
                worksheet.Range(3, 1, row, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(3, 1, row, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Thêm thông tin ngày xuất báo cáo
                row += 2;
                worksheet.Cell(row, 1).Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                worksheet.Range(row, 1, row, 4).Merge();

                // Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Flush();
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Bao_Cao_Doanh_Thu_Nam_{year}_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportMonthlyRevenueExcel(int year, int month)
        {
            // Lấy dữ liệu chi tiết theo ngày trong tháng
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var payments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Room)
                .Where(p => p.Status == "Completed" &&
                       p.PaymentDate >= startDate &&
                       p.PaymentDate <= endDate)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            // Tạo workbook và worksheet
            using (var workbook = new XLWorkbook())
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                // Worksheet 1: Tổng quan
                var overviewSheet = workbook.Worksheets.Add("Tổng quan");

                // Tạo tiêu đề
                overviewSheet.Cell(1, 1).Value = $"BÁO CÁO DOANH THU THÁNG {month}/{year}";
                overviewSheet.Cell(1, 1).Style.Font.Bold = true;
                overviewSheet.Cell(1, 1).Style.Font.FontSize = 16;
                overviewSheet.Range(1, 1, 1, 5).Merge();
                overviewSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Thông tin tổng quát
                overviewSheet.Cell(3, 1).Value = "Tổng doanh thu:";
                overviewSheet.Cell(3, 1).Style.Font.Bold = true;
                overviewSheet.Cell(3, 2).Value = (double)payments.Sum(p => p.Amount);
                overviewSheet.Cell(3, 2).Style.NumberFormat.Format = "#,##0";

                overviewSheet.Cell(4, 1).Value = "Tổng số giao dịch:";
                overviewSheet.Cell(4, 1).Style.Font.Bold = true;
                overviewSheet.Cell(4, 2).Value = payments.Count;

                overviewSheet.Cell(5, 1).Value = "Tổng số đặt phòng:";
                overviewSheet.Cell(5, 1).Style.Font.Bold = true;
                overviewSheet.Cell(5, 2).Value = payments.Select(p => p.BookingID).Distinct().Count();

                // Doanh thu theo ngày
                overviewSheet.Cell(7, 1).Value = "DOANH THU THEO NGÀY";
                overviewSheet.Cell(7, 1).Style.Font.Bold = true;
                overviewSheet.Range(7, 1, 7, 5).Merge();
                overviewSheet.Cell(7, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Header cho bảng doanh thu theo ngày
                overviewSheet.Cell(9, 1).Value = "Ngày";
                overviewSheet.Cell(9, 2).Value = "Doanh thu (VNĐ)";
                overviewSheet.Cell(9, 3).Value = "Số giao dịch";
                overviewSheet.Range(9, 1, 9, 3).Style.Font.Bold = true;
                overviewSheet.Range(9, 1, 9, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

                // Tạo dữ liệu doanh thu theo ngày
                var dailyRevenue = payments
                    .GroupBy(p => p.PaymentDate.Day)
                    .Select(g => new
                    {
                        Day = g.Key,
                        Revenue = g.Sum(p => p.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(x => x.Day)
                    .ToList();

                int row = 10;
                foreach (var dayData in dailyRevenue)
                {
                    overviewSheet.Cell(row, 1).Value = $"{dayData.Day}/{month}/{year}";
                    overviewSheet.Cell(row, 2).Value = (double)dayData.Revenue;
                    overviewSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    overviewSheet.Cell(row, 3).Value = dayData.TransactionCount;
                    row++;
                }

                // Định dạng bảng
                overviewSheet.Range(9, 1, row - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                overviewSheet.Range(9, 1, row - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Doanh thu theo loại phòng
                row += 2;
                overviewSheet.Cell(row, 1).Value = "DOANH THU THEO LOẠI PHÒNG";
                overviewSheet.Cell(row, 1).Style.Font.Bold = true;
                overviewSheet.Range(row, 1, row, 5).Merge();
                overviewSheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                row += 2;
                overviewSheet.Cell(row, 1).Value = "Loại phòng";
                overviewSheet.Cell(row, 2).Value = "Doanh thu (VNĐ)";
                overviewSheet.Cell(row, 3).Value = "Số đặt phòng";
                overviewSheet.Range(row, 1, row, 3).Style.Font.Bold = true;
                overviewSheet.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

                // Tạo dữ liệu doanh thu theo loại phòng
                var roomTypeRevenue = payments
                    .GroupBy(p => p.Booking.Room.RoomType?.TypeName ?? "Không xác định")
                    .Select(g => new
                    {
                        RoomType = g.Key,
                        Revenue = g.Sum(p => p.Amount),
                        BookingCount = g.Select(p => p.BookingID).Distinct().Count()
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList();

                row++;
                foreach (var rtData in roomTypeRevenue)
                {
                    overviewSheet.Cell(row, 1).Value = rtData.RoomType;
                    overviewSheet.Cell(row, 2).Value = (double)rtData.Revenue;
                    overviewSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    overviewSheet.Cell(row, 3).Value = rtData.BookingCount;
                    row++;
                }

                // Định dạng bảng
                int startRoomTypeRow = row - roomTypeRevenue.Count;
                overviewSheet.Range(startRoomTypeRow - 1, 1, row - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                overviewSheet.Range(startRoomTypeRow - 1, 1, row - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Doanh thu theo phương thức thanh toán
                row += 2;
                overviewSheet.Cell(row, 1).Value = "DOANH THU THEO PHƯƠNG THỨC THANH TOÁN";
                overviewSheet.Cell(row, 1).Style.Font.Bold = true;
                overviewSheet.Range(row, 1, row, 5).Merge();
                overviewSheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                row += 2;
                overviewSheet.Cell(row, 1).Value = "Phương thức";
                overviewSheet.Cell(row, 2).Value = "Doanh thu (VNĐ)";
                overviewSheet.Cell(row, 3).Value = "Số giao dịch";
                overviewSheet.Range(row, 1, row, 3).Style.Font.Bold = true;
                overviewSheet.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

                // Tạo dữ liệu doanh thu theo phương thức thanh toán
                var paymentMethodRevenue = payments
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new
                    {
                        Method = g.Key,
                        Revenue = g.Sum(p => p.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList();

                row++;
                foreach (var pmData in paymentMethodRevenue)
                {
                    overviewSheet.Cell(row, 1).Value = pmData.Method;
                    overviewSheet.Cell(row, 2).Value = (double)pmData.Revenue;
                    overviewSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    overviewSheet.Cell(row, 3).Value = pmData.Count;
                    row++;
                }

                // Định dạng bảng
                int startPaymentMethodRow = row - paymentMethodRevenue.Count;
                overviewSheet.Range(startPaymentMethodRow - 1, 1, row - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                overviewSheet.Range(startPaymentMethodRow - 1, 1, row - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Thêm thông tin ngày xuất báo cáo
                row += 2;
                overviewSheet.Cell(row, 1).Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                overviewSheet.Range(row, 1, row, 5).Merge();

                // Worksheet 2: Chi tiết giao dịch
                var detailSheet = workbook.Worksheets.Add("Chi tiết giao dịch");

                // Tạo tiêu đề
                detailSheet.Cell(1, 1).Value = $"CHI TIẾT GIAO DỊCH THÁNG {month}/{year}";
                detailSheet.Cell(1, 1).Style.Font.Bold = true;
                detailSheet.Cell(1, 1).Style.Font.FontSize = 16;
                detailSheet.Range(1, 1, 1, 8).Merge();
                detailSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Header cho bảng chi tiết
                detailSheet.Cell(3, 1).Value = "Mã GD";
                detailSheet.Cell(3, 2).Value = "Mã đặt phòng";
                detailSheet.Cell(3, 3).Value = "Khách hàng";
                detailSheet.Cell(3, 4).Value = "Phòng";
                detailSheet.Cell(3, 5).Value = "Số tiền";
                detailSheet.Cell(3, 6).Value = "Phương thức";
                detailSheet.Cell(3, 7).Value = "Ngày thanh toán";
                detailSheet.Cell(3, 8).Value = "Ghi chú";

                detailSheet.Range(3, 1, 3, 8).Style.Font.Bold = true;
                detailSheet.Range(3, 1, 3, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

                // Điền dữ liệu chi tiết
                row = 4;
                foreach (var payment in payments)
                {
                    detailSheet.Cell(row, 1).Value = payment.PaymentID;
                    detailSheet.Cell(row, 2).Value = payment.BookingID;
                    detailSheet.Cell(row, 3).Value = payment.Booking.FullName;
                    detailSheet.Cell(row, 4).Value = payment.Booking.Room.RoomName;
                    detailSheet.Cell(row, 5).Value = (double)payment.Amount;
                    detailSheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                    detailSheet.Cell(row, 6).Value = payment.PaymentMethod;
                    detailSheet.Cell(row, 7).Value = payment.PaymentDate.ToString("dd/MM/yyyy HH:mm");
                    detailSheet.Cell(row, 8).Value = payment.Notes;
                    row++;
                }

                // Định dạng bảng
                detailSheet.Range(3, 1, row - 1, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                detailSheet.Range(3, 1, row - 1, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Căn chỉnh cột
                overviewSheet.Columns().AdjustToContents();
                detailSheet.Columns().AdjustToContents();

                // Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Flush();
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Bao_Cao_Doanh_Thu_Thang_{month}_{year}_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}