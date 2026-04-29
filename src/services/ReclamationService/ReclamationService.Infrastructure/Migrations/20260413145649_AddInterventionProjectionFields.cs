using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclamationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterventionProjectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastInterventionOutcome",
                table: "Reclamations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastInterventionReportSummary",
                table: "Reclamations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReplanning",
                table: "Reclamations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastInterventionOutcome",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "LastInterventionReportSummary",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "RequiresReplanning",
                table: "Reclamations");
        }
    }
}
