using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserSessionsToThreadSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.CreateTable(
                name: "ThreadSessions",
                columns: table => new
                {
                    MessageThreadId = table.Column<string>(type: "text", nullable: false),
                    ThreadId = table.Column<string>(type: "text", nullable: false),
                    LastActivity = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadSessions", x => x.MessageThreadId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThreadSessions");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LastActivity = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ThreadId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.UserId);
                });
        }
    }
}
