using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowTime.Migrations
{
    /// <inheritdoc />
    public partial class LongToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń kolumnę
            migrationBuilder.DropColumn(
                name: "DurationInSeconds",
                table: "TimeSegments");

            // Dodaj ponownie z typem int
            migrationBuilder.AddColumn<int>(
                name: "DurationInSeconds",
                table: "TimeSegments",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN EndTime IS NULL THEN NULL ELSE DATEDIFF(second, StartTime, EndTime) END",
                stored: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Usuń kolumnę
            migrationBuilder.DropColumn(
                name: "DurationInSeconds",
                table: "TimeSegments");

            // Dodaj ponownie z typem long
            migrationBuilder.AddColumn<long>(
                name: "DurationInSeconds",
                table: "TimeSegments",
                type: "bigint",
                nullable: true,
                computedColumnSql: "CASE WHEN EndTime IS NULL THEN NULL ELSE DATEDIFF(second, StartTime, EndTime) END",
                stored: true);
        }

    }
}
