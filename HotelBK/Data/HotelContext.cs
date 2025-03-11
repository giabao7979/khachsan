using Microsoft.EntityFrameworkCore;
using HotelBK.Models;
using Microsoft.AspNetCore.Identity;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Room>()
            .Property(r => r.Price)
            .HasColumnType("decimal(18,2)");

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Admin" },
                new Role { RoleID = 2, RoleName = "Customer" },
                new Role { RoleID = 3, RoleName = "Staff" }
            );

            // Seed RoomTypes
            modelBuilder.Entity<RoomType>().HasData(
                new RoomType { RoomTypeID = 1, TypeName = "VIP", Description = "Phòng VIP cao cấp" },
                new RoomType { RoomTypeID = 2, TypeName = "Standard", Description = "Phòng tiêu chuẩn" },
                new RoomType { RoomTypeID = 3, TypeName = "Deluxe", Description = "Phòng sang trọng" }
            );
            var passwordHasher = new PasswordHasher<User>();
            var adminUser = new User
            {
                UserID = 1,
                FullName = "Admin",
                Email = "admin@example.com",
                Phone = "0123456789",
                RoleID = 1, // ID của role Admin
                PasswordHash = passwordHasher.HashPassword(null, "Admin@123")
            };

            modelBuilder.Entity<User>().HasData(adminUser);
        }
    }
}