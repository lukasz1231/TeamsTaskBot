using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaskUserManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tasks_TaskModelTaskId",
                table: "TimeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Users_UserModelUserId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TaskModelTaskId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_UserModelUserId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TaskModelTaskId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "UserModelUserId",
                table: "TimeEntries");

            migrationBuilder.CreateTable(
                name: "TaskUser",
                columns: table => new
                {
                    TasksTaskId = table.Column<string>(type: "text", nullable: false),
                    UsersUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskUser", x => new { x.TasksTaskId, x.UsersUserId });
                    table.ForeignKey(
                        name: "FK_TaskUser_Tasks_TasksTaskId",
                        column: x => x.TasksTaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskUser_Users_UsersUserId",
                        column: x => x.UsersUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskUser_UsersUserId",
                table: "TaskUser",
                column: "UsersUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskUser");

            migrationBuilder.AddColumn<string>(
                name: "TaskModelTaskId",
                table: "TimeEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserModelUserId",
                table: "TimeEntries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TaskModelTaskId",
                table: "TimeEntries",
                column: "TaskModelTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_UserModelUserId",
                table: "TimeEntries",
                column: "UserModelUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tasks_TaskModelTaskId",
                table: "TimeEntries",
                column: "TaskModelTaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Users_UserModelUserId",
                table: "TimeEntries",
                column: "UserModelUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
