using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.QuotationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistQuotationCommercialTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "manual_discount_amount",
                table: "quotation_versions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_cost",
                table: "quotation_versions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "quotation_versions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "manual_discount_amount",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "shipping_cost",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "quotation_versions");
        }
    }
}
