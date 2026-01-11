using System;
using System.Collections.Generic;
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action_type = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    changed_fields = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_info = table.Column<string>(type: "jsonb", nullable: true),
                    merged_from_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    merge_history = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material_references",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    material_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    physical_properties = table.Column<string>(type: "jsonb", nullable: true),
                    mechanical_properties = table.Column<string>(type: "jsonb", nullable: true),
                    supported_processes = table.Column<List<string>>(type: "text[]", nullable: true),
                    availability_status = table.Column<int>(type: "integer", nullable: false),
                    cached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_references", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permissions = table.Column<List<string>>(type: "text[]", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_structures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    conditions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    authorization_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_structures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_references",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    upload_service_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    uploaded_by_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    volume_cm3 = table.Column<double>(type: "double precision", nullable: true),
                    support_volume_cm3 = table.Column<double>(type: "double precision", nullable: true),
                    surface_area_cm2 = table.Column<double>(type: "double precision", nullable: true),
                    is_manifold = table.Column<bool>(type: "boolean", nullable: true),
                    triangle_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_references", x => x.id);
                    table.CheckConstraint("CK_FileReference_Entity", "(\"rfq_id\" IS NOT NULL AND \"quotation_id\" IS NULL) OR (\"rfq_id\" IS NULL AND \"quotation_id\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "internal_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internal_notes", x => x.id);
                    table.CheckConstraint("CK_InternalNote_Entity", "(\"rfq_id\" IS NOT NULL AND \"quotation_id\" IS NULL) OR (\"rfq_id\" IS NULL AND \"quotation_id\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "quotation_line_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    material_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    material_properties = table.Column<string>(type: "jsonb", nullable: true),
                    manufacturing_process = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_line_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_rfq_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    validity_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    validity_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotations", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotation_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    change_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    delivery_expectations = table.Column<string>(type: "jsonb", nullable: true),
                    special_terms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_versions_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_source = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    request_details = table.Column<string>(type: "jsonb", nullable: true),
                    assigned_staff_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    converted_to_quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqs", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqs_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rfqs_quotations_converted_to_quotation_id",
                        column: x => x.converted_to_quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_action_type",
                table: "audit_log_entries",
                column: "action_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_entity_id",
                table: "audit_log_entries",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_entity_type",
                table: "audit_log_entries",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_entity_type_entity_id_timestamp",
                table: "audit_log_entries",
                columns: new[] { "entity_type", "entity_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_timestamp",
                table: "audit_log_entries",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_user_id",
                table: "audit_log_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                table: "customers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_is_deleted",
                table: "customers",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_customers_name",
                table: "customers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone_number",
                table: "customers",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_discount_structures_quotation_version_id",
                table: "discount_structures",
                column: "quotation_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_references_quotation_id",
                table: "file_references",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_references_rfq_id",
                table: "file_references",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_references_upload_service_file_id",
                table: "file_references",
                column: "upload_service_file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internal_notes_created_at",
                table: "internal_notes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_internal_notes_quotation_id",
                table: "internal_notes",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_internal_notes_rfq_id",
                table: "internal_notes",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_references_expires_at",
                table: "material_references",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_material_references_material_service_id",
                table: "material_references",
                column: "material_service_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotation_line_items_version_id_line_number",
                table: "quotation_line_items",
                columns: new[] { "version_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_current_version_id",
                table: "quotations",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_customer_id",
                table: "quotations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_is_deleted",
                table: "quotations",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_source_rfq_id",
                table: "quotations",
                column: "source_rfq_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_status",
                table: "quotations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_validity_period_end",
                table: "quotations",
                column: "validity_period_end");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_versions_quotation_id_version_number",
                table: "quotation_versions",
                columns: new[] { "quotation_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_assigned_staff_user_id",
                table: "rfqs",
                column: "assigned_staff_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_channel_source",
                table: "rfqs",
                column: "channel_source");

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_channel_source_status_created_at",
                table: "rfqs",
                columns: new[] { "channel_source", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_converted_to_quotation_id",
                table: "rfqs",
                column: "converted_to_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_created_at",
                table: "rfqs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_customer_id",
                table: "rfqs",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_is_deleted",
                table: "rfqs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_rfqs_status",
                table: "rfqs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_staff_roles_role_name",
                table: "staff_roles",
                column: "role_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_structures_quotation_versions_quotation_version_id",
                table: "discount_structures",
                column: "quotation_version_id",
                principalTable: "quotation_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_file_references_quotations_quotation_id",
                table: "file_references",
                column: "quotation_id",
                principalTable: "quotations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_file_references_rfqs_rfq_id",
                table: "file_references",
                column: "rfq_id",
                principalTable: "rfqs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_internal_notes_quotations_quotation_id",
                table: "internal_notes",
                column: "quotation_id",
                principalTable: "quotations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_internal_notes_rfqs_rfq_id",
                table: "internal_notes",
                column: "rfq_id",
                principalTable: "rfqs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_quotation_line_items_quotation_versions_version_id",
                table: "quotation_line_items",
                column: "version_id",
                principalTable: "quotation_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_quotations_quotation_versions_current_version_id",
                table: "quotations",
                column: "current_version_id",
                principalTable: "quotation_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_quotations_rfqs_source_rfq_id",
                table: "quotations",
                column: "source_rfq_id",
                principalTable: "rfqs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_quotations_quotation_versions_current_version_id",
                table: "quotations");

            migrationBuilder.DropForeignKey(
                name: "fk_rfqs_quotations_converted_to_quotation_id",
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
