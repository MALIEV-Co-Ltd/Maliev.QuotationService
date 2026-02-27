using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.QuotationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingIdentityTypeToQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "billing_identity_type",
                table: "quotations",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Default to Corporate (1) not Personal (0)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_identity_type",
                table: "quotations");
        }
    }
}
