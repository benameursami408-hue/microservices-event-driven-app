using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclamationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReclamationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe conversion from legacy string status to int enum.
            migrationBuilder.AddColumn<int>(
                name: "StatusValue",
                table: "Reclamations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE Reclamations
SET StatusValue = CASE
    WHEN Status IN (N'Ouverte', N'OPEN', N'Open') THEN 0
    WHEN Status IN (N'ASSIGNED', N'Assigned') THEN 1
    WHEN Status IN (N'PLANNED', N'Planned') THEN 2
    WHEN Status IN (N'En cours', N'EN COURS', N'InProgress', N'INPROGRESS', N'IN_PROGRESS') THEN 3
    WHEN Status IN (N'RESOLVED', N'Resolved') THEN 4
    WHEN Status IN (N'CLOSED', N'Closed', N'Cloturee') THEN 5
    WHEN Status IN (N'CANCELLED', N'Cancelled', N'Annulee') THEN 6
    WHEN Status IN (N'REJECTED', N'Rejected', N'Rejetee') THEN 7
    ELSE 0
END
");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reclamations");

            migrationBuilder.RenameColumn(
                name: "StatusValue",
                table: "Reclamations",
                newName: "Status");

            migrationBuilder.AlterColumn<string>(
                name: "SAVName",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<long>(
                name: "SAVId",
                table: "Reclamations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedEndAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanningNote",
                table: "Reclamations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Reclamations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Reclamations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TechnicianId",
                table: "Reclamations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianName",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReclamationHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReclamationId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclamationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReclamationHistories_Reclamations_ReclamationId",
                        column: x => x.ReclamationId,
                        principalTable: "Reclamations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReclamationHistories_ReclamationId",
                table: "ReclamationHistories",
                column: "ReclamationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReclamationHistories");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PlannedEndAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PlannedStartAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PlanningNote",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "TechnicianName",
                table: "Reclamations");

            // Best-effort rollback to legacy string status.
            migrationBuilder.AddColumn<string>(
                name: "StatusText",
                table: "Reclamations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Ouverte");

            migrationBuilder.Sql(@"
UPDATE Reclamations
SET StatusText = CASE Status
    WHEN 0 THEN N'Ouverte'
    WHEN 1 THEN N'Assigned'
    WHEN 2 THEN N'Planned'
    WHEN 3 THEN N'En cours'
    WHEN 4 THEN N'Resolved'
    WHEN 5 THEN N'Closed'
    WHEN 6 THEN N'Cancelled'
    WHEN 7 THEN N'Rejected'
    ELSE N'Ouverte'
END
");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reclamations");

            migrationBuilder.RenameColumn(
                name: "StatusText",
                table: "Reclamations",
                newName: "Status");

            migrationBuilder.AlterColumn<string>(
                name: "SAVName",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SAVId",
                table: "Reclamations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
