using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.QuotationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProjectQuotationVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "generated_by_display_name",
                table: "quotation_versions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pdf_artifact_storage_path",
                table: "quotation_versions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pdf_artifact_url",
                table: "quotation_versions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pdf_generated_at",
                table: "quotation_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "project_snapshot_hash",
                table: "quotation_versions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "project_snapshot_json",
                table: "quotation_versions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_project_id",
                table: "quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_project_number",
                table: "quotations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_source_project_id",
                table: "quotations",
                column: "source_project_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_source_project_number",
                table: "quotations",
                column: "source_project_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_quotations_source_project_id",
                table: "quotations");

            migrationBuilder.DropIndex(
                name: "ix_quotations_source_project_number",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "generated_by_display_name",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "pdf_artifact_storage_path",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "pdf_artifact_url",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "pdf_generated_at",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "project_snapshot_hash",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "project_snapshot_json",
                table: "quotation_versions");

            migrationBuilder.DropColumn(
                name: "source_project_id",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "source_project_number",
                table: "quotations");
        }
    }
}
