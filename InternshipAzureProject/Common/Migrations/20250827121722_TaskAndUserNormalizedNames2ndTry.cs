using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaskAndUserNormalizedNames2ndTry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedDisplayName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql("UPDATE \"Users\" SET \"NormalizedDisplayName\" = LOWER(REPLACE(\"DisplayName\", ' ', ''));");

            // added index for searched property
            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedDisplayName",
                table: "Users",
                column: "NormalizedDisplayName");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql("UPDATE \"Tasks\" SET \"NormalizedTitle\" = LOWER(REPLACE(\"Title\", ' ', ''));");

            // added index for searched property
            migrationBuilder.CreateIndex(
                name: "IX_Tasks_NormalizedTitle",
                table: "Tasks",
                column: "NormalizedTitle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedDisplayName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "Tasks");
        }
    }
}
