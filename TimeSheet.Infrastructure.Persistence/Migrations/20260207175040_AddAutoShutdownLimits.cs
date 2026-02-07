using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoShutdownLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxCommuteHours",
                table: "Users",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxLunchHours",
                table: "Users",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxWorkHours",
                table: "Users",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxCommuteHours",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxLunchHours",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxWorkHours",
                table: "Users");
        }
    }
}
