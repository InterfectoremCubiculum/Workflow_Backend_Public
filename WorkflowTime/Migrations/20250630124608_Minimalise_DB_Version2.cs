using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class Minimalise_DB_Version2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workflow_UserWorkflow_UserWorkflowUserId",
                table: "Workflow");

            migrationBuilder.DropTable(
                name: "UserWorkflow");

            migrationBuilder.DropColumn(
                name: "UserWorkflowId",
                table: "Workflow");

            migrationBuilder.RenameColumn(
                name: "UserWorkflowUserId",
                table: "Workflow",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Workflow_UserWorkflowUserId",
                table: "Workflow",
                newName: "IX_Workflow_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Workflow",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Workflow",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Workflow_Users_UserId",
                table: "Workflow",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workflow_Users_UserId",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Workflow");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Workflow",
                newName: "UserWorkflowUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Workflow_UserId",
                table: "Workflow",
                newName: "IX_Workflow_UserWorkflowUserId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Workflow",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "UserWorkflowId",
                table: "Workflow",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "UserWorkflow",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWorkflow", x => x.UserId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Workflow_UserWorkflow_UserWorkflowUserId",
                table: "Workflow",
                column: "UserWorkflowUserId",
                principalTable: "UserWorkflow",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
