using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetWorkHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TargetWorkHours",
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
                name: "TargetWorkHours",
                table: "Users");
        }
    }
}
