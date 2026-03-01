using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foundry.Configuration.Infrastructure.Migrations;

/// <inheritdoc />
public partial class VariantsToOwnedTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "variants",
            schema: "configuration",
            table: "feature_flags");

        migrationBuilder.CreateTable(
            name: "feature_flag_variants",
            schema: "configuration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                weight = table.Column<int>(type: "integer", nullable: false),
                FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feature_flag_variants", x => x.Id);
                table.ForeignKey(
                    name: "FK_feature_flag_variants_feature_flags_FeatureFlagId",
                    column: x => x.FeatureFlagId,
                    principalSchema: "configuration",
                    principalTable: "feature_flags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_feature_flag_variants_FeatureFlagId",
            schema: "configuration",
            table: "feature_flag_variants",
            column: "FeatureFlagId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "feature_flag_variants",
            schema: "configuration");

        migrationBuilder.AddColumn<string>(
            name: "variants",
            schema: "configuration",
            table: "feature_flags",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }
}
