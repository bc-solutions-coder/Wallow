using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foundry.Communications.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPushChannel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "device_registrations",
            schema: "communications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_device_registrations", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "push_messages",
            schema: "communications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                body = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_push_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tenant_push_configurations",
            schema: "communications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                encrypted_credentials = table.Column<string>(type: "text", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tenant_push_configurations", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_device_registrations_tenant_id",
            schema: "communications",
            table: "device_registrations",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_device_registrations_token_tenant_id",
            schema: "communications",
            table: "device_registrations",
            columns: new[] { "token", "tenant_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_push_messages_created_at",
            schema: "communications",
            table: "push_messages",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "IX_push_messages_status",
            schema: "communications",
            table: "push_messages",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_push_messages_tenant_id",
            schema: "communications",
            table: "push_messages",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_tenant_push_configurations_tenant_id",
            schema: "communications",
            table: "tenant_push_configurations",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_tenant_push_configurations_tenant_id_platform",
            schema: "communications",
            table: "tenant_push_configurations",
            columns: new[] { "tenant_id", "platform" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "device_registrations",
            schema: "communications");

        migrationBuilder.DropTable(
            name: "push_messages",
            schema: "communications");

        migrationBuilder.DropTable(
            name: "tenant_push_configurations",
            schema: "communications");
    }
}
