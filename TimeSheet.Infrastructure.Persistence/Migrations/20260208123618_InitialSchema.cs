using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CommuteDirection = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    TelegramUsername = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    UtcOffsetMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    MaxWorkHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MaxCommuteHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MaxLunchHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    LunchReminderHour = table.Column<int>(type: "INTEGER", nullable: true),
                    LunchReminderMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetWorkHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    ForgotShutdownThresholdPercent = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_StartedAt",
                table: "TrackingSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_UserId",
                table: "TrackingSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_UserId_EndedAt",
                table: "TrackingSessions",
                columns: new[] { "UserId", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_UserId_StartedAt",
                table: "TrackingSessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users",
                column: "TelegramUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackingSessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
