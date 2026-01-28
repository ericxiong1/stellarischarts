using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarisCharts.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Adjective = table.Column<string>(type: "text", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    GovernmentType = table.Column<string>(type: "text", nullable: false),
                    Authority = table.Column<string>(type: "text", nullable: false),
                    Personality = table.Column<string>(type: "text", nullable: false),
                    GraphicalCulture = table.Column<string>(type: "text", nullable: false),
                    Capital = table.Column<int>(type: "integer", nullable: false),
                    MilitaryPower = table.Column<decimal>(type: "numeric", nullable: false),
                    EconomyPower = table.Column<decimal>(type: "numeric", nullable: false),
                    TechPower = table.Column<decimal>(type: "numeric", nullable: false),
                    FleetSize = table.Column<int>(type: "integer", nullable: false),
                    EmpireSize = table.Column<int>(type: "integer", nullable: false),
                    NumSapientPops = table.Column<long>(type: "bigint", nullable: false),
                    VictoryRank = table.Column<int>(type: "integer", nullable: false),
                    VictoryScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    GameDate = table.Column<string>(type: "text", nullable: false),
                    Tick = table.Column<int>(type: "integer", nullable: false),
                    MilitaryPower = table.Column<decimal>(type: "numeric", nullable: false),
                    EconomyPower = table.Column<decimal>(type: "numeric", nullable: false),
                    TechPower = table.Column<decimal>(type: "numeric", nullable: false),
                    FleetSize = table.Column<int>(type: "integer", nullable: false),
                    EmpireSize = table.Column<int>(type: "integer", nullable: false),
                    NumSapientPops = table.Column<long>(type: "bigint", nullable: false),
                    VictoryRank = table.Column<int>(type: "integer", nullable: false),
                    VictoryScore = table.Column<decimal>(type: "numeric", nullable: false),
                    SnapshotTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Snapshots_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetLineItems_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetLineItems_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLineItems_CountryId",
                table: "BudgetLineItems",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLineItems_SnapshotId_CountryId_Section",
                table: "BudgetLineItems",
                columns: new[] { "SnapshotId", "CountryId", "Section" });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_CountryId",
                table: "Snapshots",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetLineItems");

            migrationBuilder.DropTable(
                name: "Snapshots");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
