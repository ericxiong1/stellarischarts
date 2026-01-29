using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryMetaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AscensionPerks",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Civics",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiplomaticStance",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiplomaticWeight",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FederationType",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectStatus",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TraditionTrees",
                table: "Countries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AscensionPerks",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Civics",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "DiplomaticStance",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "DiplomaticWeight",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "FederationType",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "SubjectStatus",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "TraditionTrees",
                table: "Countries");
        }
    }
}
