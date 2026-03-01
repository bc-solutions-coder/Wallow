using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foundry.Configuration.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "configuration");

        migrationBuilder.CreateTable(
            name: "custom_field_definitions",
            schema: "configuration",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                field_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                field_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                validation_rules = table.Column<string>(type: "jsonb", nullable: true),
                options = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_custom_field_definitions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "feature_flags",
            schema: "configuration",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                flag_type = table.Column<int>(type: "integer", nullable: false),
                default_enabled = table.Column<bool>(type: "boolean", nullable: false),
                rollout_percentage = table.Column<int>(type: "integer", nullable: true),
                variants = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                default_variant = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feature_flags", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "feature_flag_overrides",
            schema: "configuration",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_enabled = table.Column<bool>(type: "boolean", nullable: true),
                variant = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feature_flag_overrides", x => x.id);
                table.ForeignKey(
                    name: "FK_feature_flag_overrides_feature_flags_flag_id",
                    column: x => x.flag_id,
                    principalSchema: "configuration",
                    principalTable: "feature_flags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_custom_field_definitions_tenant_entity_active",
            schema: "configuration",
            table: "custom_field_definitions",
            columns: new[] { "tenant_id", "entity_type", "is_active" });

        migrationBuilder.CreateIndex(
            name: "ix_custom_field_definitions_tenant_entity_key",
            schema: "configuration",
            table: "custom_field_definitions",
            columns: new[] { "tenant_id", "entity_type", "field_key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_custom_field_definitions_tenant_id",
            schema: "configuration",
            table: "custom_field_definitions",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_feature_flag_overrides_flag_id_tenant_id_user_id",
            schema: "configuration",
            table: "feature_flag_overrides",
            columns: new[] { "flag_id", "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "IX_feature_flag_overrides_tenant_id",
            schema: "configuration",
            table: "feature_flag_overrides",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_feature_flag_overrides_user_id",
            schema: "configuration",
            table: "feature_flag_overrides",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_feature_flags_key",
            schema: "configuration",
            table: "feature_flags",
            column: "key",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "custom_field_definitions",
            schema: "configuration");

        migrationBuilder.DropTable(
            name: "feature_flag_overrides",
            schema: "configuration");

        migrationBuilder.DropTable(
            name: "feature_flags",
            schema: "configuration");
    }
}
