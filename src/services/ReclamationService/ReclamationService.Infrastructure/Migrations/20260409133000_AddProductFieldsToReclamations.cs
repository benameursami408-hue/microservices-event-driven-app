using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclamationService.Infrastructure.Migrations
{
    public partial class AddProductFieldsToReclamations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Reclamations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Reclamations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductImageUrl",
                table: "Reclamations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Reclamations",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductReference",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseProofUrl",
                table: "Reclamations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "Reclamations",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Reclamations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ProductImageUrl",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "ProductReference",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "PurchaseProofUrl",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "Reclamations");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Reclamations");
        }
    }
}
