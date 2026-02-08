using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSheet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use IF NOT EXISTS to handle databases where the table was created outside migrations
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "TimeEntries" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_TimeEntries" PRIMARY KEY,
                    "UserId" INTEGER NOT NULL,
                    "State" TEXT NOT NULL,
                    "StartedAt" TEXT NOT NULL,
                    "EndedAt" TEXT NULL,
                    "CommuteDirection" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL
                );
                """);
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TimeEntries_StartedAt" ON "TimeEntries" ("StartedAt");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TimeEntries_UserId" ON "TimeEntries" ("UserId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_TimeEntries_UserId_StartedAt" ON "TimeEntries" ("UserId", "StartedAt");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeEntries");
        }
    }
}
