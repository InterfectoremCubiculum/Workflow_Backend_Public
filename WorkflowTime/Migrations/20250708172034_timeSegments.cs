using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class timeSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkLogModels_Users_UserId",
                table: "WorkLogModels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkLogModels",
                table: "WorkLogModels");

            migrationBuilder.RenameTable(
                name: "WorkLogModels",
                newName: "TimeSegments");

            migrationBuilder.RenameColumn(
                name: "isDeleted",
                table: "Users",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "isDeleted",
                table: "DayOffRequests",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "isDeleted",
                table: "TimeSegments",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "WorkingStatus",
                table: "TimeSegments",
                newName: "TimeSegmentType");

            migrationBuilder.RenameIndex(
                name: "IX_WorkLogModels_UserId",
                table: "TimeSegments",
                newName: "IX_TimeSegments_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimeSegments",
                table: "TimeSegments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSegments_Users_UserId",
                table: "TimeSegments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSegments_Users_UserId",
                table: "TimeSegments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimeSegments",
                table: "TimeSegments");

            migrationBuilder.RenameTable(
                name: "TimeSegments",
                newName: "WorkLogModels");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Users",
                newName: "isDeleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "DayOffRequests",
                newName: "isDeleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "WorkLogModels",
                newName: "isDeleted");

            migrationBuilder.RenameColumn(
                name: "TimeSegmentType",
                table: "WorkLogModels",
                newName: "WorkingStatus");

            migrationBuilder.RenameIndex(
                name: "IX_TimeSegments_UserId",
                table: "WorkLogModels",
                newName: "IX_WorkLogModels_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkLogModels",
                table: "WorkLogModels",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogModels_Users_UserId",
                table: "WorkLogModels",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
