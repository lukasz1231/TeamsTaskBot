using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TimeEntriesStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "AadId",
                table: "Users",
                newName: "UserId");

            migrationBuilder.AddColumn<decimal>(
                name: "DurationInHours",
                table: "TimeEntries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndTime",
                table: "TimeEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartTime",
                table: "TimeEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "TimeEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskModelTaskId",
                table: "TimeEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TimeEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserModelUserId",
                table: "TimeEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BucketId",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueDate",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentComplete",
                table: "Tasks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlanId",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TaskId",
                table: "TimeEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TaskModelTaskId",
                table: "TimeEntries",
                column: "TaskModelTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_UserModelUserId",
                table: "TimeEntries",
                column: "UserModelUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tasks_TaskId",
                table: "TimeEntries",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Tasks_TaskModelTaskId",
                table: "TimeEntries",
                column: "TaskModelTaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Users_UserId",
                table: "TimeEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Users_UserModelUserId",
                table: "TimeEntries",
                column: "UserModelUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tasks_TaskId",
                table: "TimeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Tasks_TaskModelTaskId",
                table: "TimeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Users_UserId",
                table: "TimeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Users_UserModelUserId",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TaskId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TaskModelTaskId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_UserModelUserId",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DurationInHours",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TaskModelTaskId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "UserModelUserId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "BucketId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PercentComplete",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Users",
                newName: "AadId");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");
        }
    }
}
