using HotelBK.Data; // Import namespace của DbContext
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using HotelBK.Models;
using HotelBK.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kết nối SQL Server
builder.Services.AddDbContext<HotelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HotelManagementDB")));

// Cấu hình dịch vụ MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Thêm dịch vụ xác thực cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

// Thêm service Password Hasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Thêm các service vào Program.cs
builder.Services.AddScoped<IBookingService, BookingServices>();
builder.Services.AddScoped<IRoomService, RoomService>();

// Đảm bảo middleware xác thực được kích hoạt
app.UseAuthentication();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// ⚡ Thêm route cho Areas
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
