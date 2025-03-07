using HotelBK.Data; // Import namespace của DbContext
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using HotelBK.Models;
using HotelBK.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kết nối cơ sở dữ liệu
builder.Services.AddDbContext<HotelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HotelManagementDB")));

// Thêm dịch vụ xác thực TRƯỚC KHI xây dựng ứng dụng
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(3);
    });

// Thêm dịch vụ PasswordHasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Thêm các dịch vụ nghiệp vụ
builder.Services.AddScoped<IBookingService, BookingServices>();
builder.Services.AddScoped<IRoomService, RoomService>();

// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Quan trọng: Gọi UseAuthentication TRƯỚC UseAuthorization
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Phải đặt trước Authorization
app.UseAuthorization();

// Cấu hình routes
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}"
    );

    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );
});

app.Run();
