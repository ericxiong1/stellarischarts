using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalSpeciesPopulations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalSpeciesPopulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotId = table.Column<int>(type: "integer", nullable: false),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    SpeciesName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSpeciesPopulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalSpeciesPopulations_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalSpeciesPopulations_SnapshotId",
                table: "GlobalSpeciesPopulations",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalSpeciesPopulations_SnapshotId_SpeciesId",
                table: "GlobalSpeciesPopulations",
                columns: new[] { "SnapshotId", "SpeciesId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalSpeciesPopulations");
        }
    }
}
