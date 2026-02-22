using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserComplianceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserComplianceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ThresholdHours = table.Column<double>(type: "REAL", nullable: false),
                    ClockInDefinition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ClockOutDefinition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FixedClockIn = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    FixedClockOut = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserComplianceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserComplianceRules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserComplianceRules_UserId_RuleType",
                table: "UserComplianceRules",
                columns: new[] { "UserId", "RuleType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserComplianceRules");
        }
    }
}
