using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBK.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialRequestField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingServices");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.AddColumn<string>(
                name: "SpecialRequest",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 3, 12, 11, 5, 53, 584, DateTimeKind.Local).AddTicks(1180), "AQAAAAIAAYagAAAAEKvgxwRO5NO/fR9tcFpxbQ+FlqnJEE5SACORtbR6bM/UyVHLi258kUIvQ2+zBETjSQ==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecialRequest",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    ServiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.ServiceID);
                    table.ForeignKey(
                        name: "FK_Services_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingServices",
                columns: table => new
                {
                    BookingServiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServices", x => x.BookingServiceID);
                    table.ForeignKey(
                        name: "FK_BookingServices_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingServices_Services_ServiceID",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 3, 7, 14, 48, 41, 166, DateTimeKind.Local).AddTicks(9792), "AQAAAAIAAYagAAAAEOazl97nu24brAJ1V60xYp+/TGDJDaWP6BYvRLcIWBdBccaU6ThJlc4jsaGDu7S+Vw==" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_BookingID",
                table: "BookingServices",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_ServiceID",
                table: "BookingServices",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CreatedBy",
                table: "Services",
                column: "CreatedBy");
        }
    }
}
