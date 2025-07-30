using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class Minimalise_DB_Vesrion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWorkflow_UserProjects_UserId_ProjectId",
                table: "UserWorkflow");

            migrationBuilder.DropForeignKey(
                name: "FK_Workflow_UserWorkflow_UserWorkflowUserId_UserWorkflowProjectId",
                table: "Workflow");

            migrationBuilder.DropTable(
                name: "UserProjects");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Workflow_UserWorkflowUserId_UserWorkflowProjectId",
                table: "Workflow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserWorkflow",
                table: "UserWorkflow");

            migrationBuilder.DropColumn(
                name: "UserWorkflowProjectId",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "UserWorkflow");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserWorkflow",
                table: "UserWorkflow",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_UserWorkflowUserId",
                table: "Workflow",
                column: "UserWorkflowUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workflow_UserWorkflow_UserWorkflowUserId",
                table: "Workflow",
                column: "UserWorkflowUserId",
                principalTable: "UserWorkflow",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workflow_UserWorkflow_UserWorkflowUserId",
                table: "Workflow");

            migrationBuilder.DropIndex(
                name: "IX_Workflow_UserWorkflowUserId",
                table: "Workflow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserWorkflow",
                table: "UserWorkflow");

            migrationBuilder.AddColumn<int>(
                name: "UserWorkflowProjectId",
                table: "Workflow",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "UserWorkflow",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserWorkflow",
                table: "UserWorkflow",
                columns: new[] { "UserId", "ProjectId" });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProjects",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProjects", x => new { x.UserId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_UserProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProjects_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_UserWorkflowUserId_UserWorkflowProjectId",
                table: "Workflow",
                columns: new[] { "UserWorkflowUserId", "UserWorkflowProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProjects_ProjectId",
                table: "UserProjects",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWorkflow_UserProjects_UserId_ProjectId",
                table: "UserWorkflow",
                columns: new[] { "UserId", "ProjectId" },
                principalTable: "UserProjects",
                principalColumns: new[] { "UserId", "ProjectId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Workflow_UserWorkflow_UserWorkflowUserId_UserWorkflowProjectId",
                table: "Workflow",
                columns: new[] { "UserWorkflowUserId", "UserWorkflowProjectId" },
                principalTable: "UserWorkflow",
                principalColumns: new[] { "UserId", "ProjectId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
