using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWarStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    WarId = table.Column<int>(type: "integer", nullable: false),
                    WarName = table.Column<string>(type: "text", nullable: false),
                    WarStartDate = table.Column<string>(type: "text", nullable: false),
                    WarLength = table.Column<string>(type: "text", nullable: false),
                    AttackerWarExhaustion = table.Column<decimal>(type: "numeric", nullable: false),
                    DefenderWarExhaustion = table.Column<decimal>(type: "numeric", nullable: false),
                    Attackers = table.Column<string>(type: "text", nullable: false),
                    Defenders = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarStatuses_CountryId",
                table: "WarStatuses",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarStatuses");
        }
    }
}
