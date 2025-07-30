using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class added_Soft_delete3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isActive",
                table: "Users",
                newName: "isDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isDeleted",
                table: "Users",
                newName: "isActive");
        }
    }
}
