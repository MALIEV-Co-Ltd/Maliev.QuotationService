using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.QuotationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeometryMetricsToFileReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_manifold",
                table: "file_references",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "support_volume_cm3",
                table: "file_references",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "surface_area_cm2",
                table: "file_references",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "triangle_count",
                table: "file_references",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "volume_cm3",
                table: "file_references",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_manifold",
                table: "file_references");

            migrationBuilder.DropColumn(
                name: "support_volume_cm3",
                table: "file_references");

            migrationBuilder.DropColumn(
                name: "surface_area_cm2",
                table: "file_references");

            migrationBuilder.DropColumn(
                name: "triangle_count",
                table: "file_references");

            migrationBuilder.DropColumn(
                name: "volume_cm3",
                table: "file_references");
        }
    }
}
