using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidaysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_UserId",
                table: "Holidays",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_UserId_DateRange",
                table: "Holidays",
                columns: new[] { "UserId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_UserId_StartDate",
                table: "Holidays",
                columns: new[] { "UserId", "StartDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");
        }
    }
}
