using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.QuotationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ChangedFields = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactInfo = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    MergedFromIds = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    MergeHistory = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "material_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaterialCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhysicalProperties = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    MechanicalProperties = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    SupportedProcesses = table.Column<List<string>>(type: "text[]", nullable: true),
                    AvailabilityStatus = table.Column<int>(type: "integer", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_references", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Permissions = table.Column<List<string>>(type: "text[]", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discount_structures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Conditions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuthorizationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_structures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadServiceFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UploadedByUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_references", x => x.Id);
                    table.CheckConstraint("CK_FileReference_Entity", "(\"RfqId\" IS NOT NULL AND \"QuotationId\" IS NULL) OR (\"RfqId\" IS NULL AND \"QuotationId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "internal_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_notes", x => x.Id);
                    table.CheckConstraint("CK_InternalNote_Entity", "(\"RfqId\" IS NOT NULL AND \"QuotationId\" IS NULL) OR (\"RfqId\" IS NULL AND \"QuotationId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "quotation_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    MaterialServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaterialProperties = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    ManufacturingProcess = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_line_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRfqId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidityPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidityPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotations_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotation_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ChangeSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DeliveryExpectations = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    SpecialTerms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_versions_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelSource = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestDetails = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    AssignedStaffUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConvertedToQuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfqs_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfqs_quotations_ConvertedToQuotationId",
                        column: x => x.ConvertedToQuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_ActionType",
                table: "audit_log_entries",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_EntityId",
                table: "audit_log_entries",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_EntityType",
                table: "audit_log_entries",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_EntityType_EntityId_Timestamp",
                table: "audit_log_entries",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_Timestamp",
                table: "audit_log_entries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_UserId",
                table: "audit_log_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Email",
                table: "customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_IsDeleted",
                table: "customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Name",
                table: "customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_customers_PhoneNumber",
                table: "customers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_discount_structures_QuotationVersionId",
                table: "discount_structures",
                column: "QuotationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_file_references_QuotationId",
                table: "file_references",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_file_references_RfqId",
                table: "file_references",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_file_references_UploadServiceFileId",
                table: "file_references",
                column: "UploadServiceFileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_internal_notes_CreatedAt",
                table: "internal_notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_internal_notes_QuotationId",
                table: "internal_notes",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_notes_RfqId",
                table: "internal_notes",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_material_references_ExpiresAt",
                table: "material_references",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_material_references_MaterialServiceId",
                table: "material_references",
                column: "MaterialServiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotation_line_items_VersionId_LineNumber",
                table: "quotation_line_items",
                columns: new[] { "VersionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotations_CurrentVersionId",
                table: "quotations",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_CustomerId",
                table: "quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_IsDeleted",
                table: "quotations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_SourceRfqId",
                table: "quotations",
                column: "SourceRfqId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_Status",
                table: "quotations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_ValidityPeriodEnd",
                table: "quotations",
                column: "ValidityPeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_versions_QuotationId_VersionNumber",
                table: "quotation_versions",
                columns: new[] { "QuotationId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_AssignedStaffUserId",
                table: "rfqs",
                column: "AssignedStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_ChannelSource",
                table: "rfqs",
                column: "ChannelSource");

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_ChannelSource_Status_CreatedAt",
                table: "rfqs",
                columns: new[] { "ChannelSource", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_ConvertedToQuotationId",
                table: "rfqs",
                column: "ConvertedToQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_CreatedAt",
                table: "rfqs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_CustomerId",
                table: "rfqs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_IsDeleted",
                table: "rfqs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_rfqs_Status",
                table: "rfqs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_staff_roles_RoleName",
                table: "staff_roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_discount_structures_quotation_versions_QuotationVersionId",
                table: "discount_structures",
                column: "QuotationVersionId",
                principalTable: "quotation_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_file_references_quotations_QuotationId",
                table: "file_references",
                column: "QuotationId",
                principalTable: "quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_file_references_rfqs_RfqId",
                table: "file_references",
                column: "RfqId",
                principalTable: "rfqs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_notes_quotations_QuotationId",
                table: "internal_notes",
                column: "QuotationId",
                principalTable: "quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_notes_rfqs_RfqId",
                table: "internal_notes",
                column: "RfqId",
                principalTable: "rfqs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quotation_line_items_quotation_versions_VersionId",
                table: "quotation_line_items",
                column: "VersionId",
                principalTable: "quotation_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quotations_quotation_versions_CurrentVersionId",
                table: "quotations",
                column: "CurrentVersionId",
                principalTable: "quotation_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quotations_rfqs_SourceRfqId",
                table: "quotations",
                column: "SourceRfqId",
                principalTable: "rfqs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quotations_quotation_versions_CurrentVersionId",
                table: "quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_rfqs_quotations_ConvertedToQuotationId",
                table: "rfqs");

            migrationBuilder.DropTable(
                name: "audit_log_entries");

            migrationBuilder.DropTable(
                name: "discount_structures");

            migrationBuilder.DropTable(
                name: "file_references");

            migrationBuilder.DropTable(
                name: "internal_notes");

            migrationBuilder.DropTable(
                name: "material_references");

            migrationBuilder.DropTable(
                name: "quotation_line_items");

            migrationBuilder.DropTable(
                name: "staff_roles");

            migrationBuilder.DropTable(
                name: "quotation_versions");

            migrationBuilder.DropTable(
                name: "quotations");

            migrationBuilder.DropTable(
                name: "rfqs");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
