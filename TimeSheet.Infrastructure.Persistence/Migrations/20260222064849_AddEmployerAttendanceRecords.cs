using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerAttendanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployerAttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ClockIn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClockOut = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WorkingHours = table.Column<double>(type: "REAL", nullable: true),
                    HasConflict = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConflictType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EventTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ImportBatchId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerAttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployerAttendanceRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployerImportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordsImported = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDaysProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployerImportLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerAttendanceRecords_UserId_Date",
                table: "EmployerAttendanceRecords",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployerImportLogs_UserId",
                table: "EmployerImportLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployerAttendanceRecords");

            migrationBuilder.DropTable(
                name: "EmployerImportLogs");
        }
    }
}
