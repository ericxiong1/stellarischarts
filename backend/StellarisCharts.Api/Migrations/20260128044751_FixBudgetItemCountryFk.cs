using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixBudgetItemCountryFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLineItems_Countries_CountryId",
                table: "BudgetLineItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLineItems_Countries_CountryId",
                table: "BudgetLineItems",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "CountryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLineItems_Countries_CountryId",
                table: "BudgetLineItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLineItems_Countries_CountryId",
                table: "BudgetLineItems",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
