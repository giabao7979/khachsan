using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBK.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "CreatedAt", "Email", "FullName", "PasswordHash", "Phone", "RoleID" },
                values: new object[] { 1, new DateTime(2025, 3, 7, 14, 48, 41, 166, DateTimeKind.Local).AddTicks(9792), "admin@example.com", "Admin", "AQAAAAIAAYagAAAAEOazl97nu24brAJ1V60xYp+/TGDJDaWP6BYvRLcIWBdBccaU6ThJlc4jsaGDu7S+Vw==", "0123456789", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1);
        }
    }
}
