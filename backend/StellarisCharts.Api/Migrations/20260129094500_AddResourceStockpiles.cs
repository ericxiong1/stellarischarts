using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceStockpiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceStockpiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceStockpiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceStockpiles_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceStockpiles_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStockpiles_SnapshotId_CountryId_ResourceType",
                table: "ResourceStockpiles",
                columns: new[] { "SnapshotId", "CountryId", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStockpiles_SnapshotId",
                table: "ResourceStockpiles",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStockpiles_CountryId",
                table: "ResourceStockpiles",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceStockpiles_SnapshotId",
                table: "ResourceStockpiles");

            migrationBuilder.DropIndex(
                name: "IX_ResourceStockpiles_CountryId",
                table: "ResourceStockpiles");

            migrationBuilder.DropTable(
                name: "ResourceStockpiles");
        }
    }
}
