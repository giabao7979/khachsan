using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelBK.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Customer" },
                    { 3, "Staff" }
                });

            migrationBuilder.InsertData(
                table: "RoomTypes",
                columns: new[] { "RoomTypeID", "Description", "TypeName" },
                values: new object[,]
                {
                    { 1, "Phòng VIP cao cấp", "VIP" },
                    { 2, "Phòng tiêu chuẩn", "Standard" },
                    { 3, "Phòng sang trọng", "Deluxe" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RoomTypes",
                keyColumn: "RoomTypeID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RoomTypes",
                keyColumn: "RoomTypeID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RoomTypes",
                keyColumn: "RoomTypeID",
                keyValue: 3);
        }
    }
}
