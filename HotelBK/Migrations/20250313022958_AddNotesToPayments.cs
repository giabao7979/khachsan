using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBK.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SpecialRequest",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 3, 13, 9, 29, 57, 145, DateTimeKind.Local).AddTicks(76), "AQAAAAIAAYagAAAAEObt96aWNoPpDdjVPJiCVCj1D5VN8YGUt74hKLqo9AoujtXGTzyfZuaEo43IJYMqQA==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "SpecialRequest",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 3, 12, 11, 5, 53, 584, DateTimeKind.Local).AddTicks(1180), "AQAAAAIAAYagAAAAEKvgxwRO5NO/fR9tcFpxbQ+FlqnJEE5SACORtbR6bM/UyVHLi258kUIvQ2+zBETjSQ==" });
        }
    }
}
