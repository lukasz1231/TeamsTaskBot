using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LocalIntTaskIdAndRemovedDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tasks_TaskId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskUser_Tasks_TasksTaskId",
                table: "TaskUser");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tasks_TaskId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TaskId",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskUser",
                table: "TaskUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TaskId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TasksTaskId",
                table: "TaskUser");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Comments");

            migrationBuilder.AddColumn<int>(
                name: "LocalTaskId",
                table: "TimeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TasksId",
                table: "TaskUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "TaskId",
                table: "Tasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "LocalTaskId",
                table: "Comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskUser",
                table: "TaskUser",
                columns: new[] { "TasksId", "UsersUserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_LocalTaskId",
                table: "TimeEntries",
                column: "LocalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_LocalTaskId",
                table: "Comments",
                column: "LocalTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tasks_LocalTaskId",
                table: "Comments",
                column: "LocalTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskUser_Tasks_TasksId",
                table: "TaskUser",
                column: "TasksId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tasks_LocalTaskId",
                table: "TimeEntries",
                column: "LocalTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tasks_LocalTaskId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskUser_Tasks_TasksId",
                table: "TaskUser");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tasks_LocalTaskId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_LocalTaskId",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskUser",
                table: "TaskUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Comments_LocalTaskId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "LocalTaskId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TasksId",
                table: "TaskUser");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LocalTaskId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "TimeEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TasksTaskId",
                table: "TaskUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TaskId",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskUser",
                table: "TaskUser",
                columns: new[] { "TasksTaskId", "UsersUserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TaskId",
                table: "TimeEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TaskId",
                table: "Comments",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tasks_TaskId",
                table: "Comments",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskUser_Tasks_TasksTaskId",
                table: "TaskUser",
                column: "TasksTaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tasks_TaskId",
                table: "TimeEntries",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
