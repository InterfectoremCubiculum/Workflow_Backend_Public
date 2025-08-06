using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class createdAt_TimeSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamsConversationId",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TimeSegments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TimeSegments");

            migrationBuilder.AddColumn<string>(
                name: "TeamsConversationId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
