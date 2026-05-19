using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclamationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReclamationOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClaimedBySavId",
                table: "Reclamations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedBySavName",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanningRequestedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reclamations_ClaimedBySavId",
                table: "Reclamations",
                column: "ClaimedBySavId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUsers_Role",
                table: "ServiceUsers",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceUsers");

            migrationBuilder.DropIndex(
                name: "IX_Reclamations_ClaimedBySavId",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ClaimedBySavId",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ClaimedBySavName",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PlanningRequestedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "Reclamations");
        }
    }
}
