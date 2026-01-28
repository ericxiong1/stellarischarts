using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixSnapshotCountryFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Snapshots_Countries_CountryId",
                table: "Snapshots");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Countries_CountryId",
                table: "Countries",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Snapshots_Countries_CountryId",
                table: "Snapshots",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "CountryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Snapshots_Countries_CountryId",
                table: "Snapshots");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Countries_CountryId",
                table: "Countries");

            migrationBuilder.AddForeignKey(
                name: "FK_Snapshots_Countries_CountryId",
                table: "Snapshots",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
