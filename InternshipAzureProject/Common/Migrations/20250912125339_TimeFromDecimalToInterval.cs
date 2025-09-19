using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TimeFromDecimalToInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationInHours",
                table: "TimeEntries");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "TimeEntries",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "TimeEntries");

            migrationBuilder.AddColumn<decimal>(
                name: "DurationInHours",
                table: "TimeEntries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
