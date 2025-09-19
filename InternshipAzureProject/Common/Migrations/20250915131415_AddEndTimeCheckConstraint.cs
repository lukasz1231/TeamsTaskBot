using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEndTimeCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CHK_EndTime_After_StartTime",
                table: "TimeEntries",
                sql: "\"EndTime\" >= \"StartTime\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_EndTime_After_StartTime",
                table: "TimeEntries");
        }
    }
}
