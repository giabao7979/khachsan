using Microsoft.EntityFrameworkCore;
using HotelBK.Models;

namespace HotelBK.Data
{
    public class HotelContext : DbContext
    {
        public HotelContext(DbContextOptions<HotelContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BookingService> BookingServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cho RoomType để khớp với bảng trong database
            modelBuilder.Entity<RoomType>().ToTable("RoomType");
            modelBuilder.Entity<RoomType>().HasKey(r => r.RoomTypeID);

            // Các cấu hình khác
        }
    }
}