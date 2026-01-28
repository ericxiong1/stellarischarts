using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeciesPopulations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpeciesPopulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    SpeciesName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesPopulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpeciesPopulations_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpeciesPopulations_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesPopulations_CountryId",
                table: "SpeciesPopulations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesPopulations_SnapshotId",
                table: "SpeciesPopulations",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesPopulations_SnapshotId_CountryId_SpeciesId",
                table: "SpeciesPopulations",
                columns: new[] { "SnapshotId", "CountryId", "SpeciesId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeciesPopulations");
        }
    }
}
