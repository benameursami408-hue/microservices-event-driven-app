using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclamationService.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPriorityAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiPriorityAnalyses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReclamationId = table.Column<long>(type: "bigint", nullable: false),
                    SuggestedPriority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    SlaRisk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetectedKeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPriorityAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiPriorityAnalyses_Reclamations_ReclamationId",
                        column: x => x.ReclamationId,
                        principalTable: "Reclamations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPriorityAnalyses_ReclamationId",
                table: "AiPriorityAnalyses",
                column: "ReclamationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiPriorityAnalyses");
        }
    }
}
