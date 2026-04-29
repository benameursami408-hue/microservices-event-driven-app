using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclamationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaAndPriorityTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseDeadline",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FollowUpCount",
                table: "Reclamations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocking",
                table: "Reclamations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManualPriorityOverride",
                table: "Reclamations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualPriorityOverrideReason",
                table: "Reclamations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanningDeadline",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriorityReasons",
                table: "Reclamations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityScore",
                table: "Reclamations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrioritySource",
                table: "Reclamations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriorityUpdatedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolutionDeadline",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "Reclamations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaBreachedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaStatus",
                table: "Reclamations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstResponseDeadline",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "FollowUpCount",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "IsBlocking",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ManualPriorityOverride",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ManualPriorityOverrideReason",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PlanningDeadline",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PriorityReasons",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PrioritySource",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PriorityUpdatedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ResolutionDeadline",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "SlaBreachedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "SlaStatus",
                table: "Reclamations");
        }
    }
}
