using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class added_requestAttention_to_timesegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequestAction",
                table: "TimeSegments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestAction",
                table: "TimeSegments");
        }
    }
}
