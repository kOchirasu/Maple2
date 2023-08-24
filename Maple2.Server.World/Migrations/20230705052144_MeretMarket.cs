using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maple2.Server.World.Migrations
{
    /// <inheritdoc />
    public partial class MeretMarket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SubType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Language = table.Column<int>(type: "int", nullable: false),
                    BeginTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banner", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "premium-market-entry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    TabId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Rarity = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    BonusQuantity = table.Column<int>(type: "int", nullable: false),
                    ItemDuration = table.Column<int>(type: "int", nullable: false),
                    CurrencyType = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    SalePrice = table.Column<long>(type: "bigint", nullable: false),
                    SellBeginTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SellEndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SalesCount = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    JobRequirement = table.Column<int>(type: "int", nullable: false),
                    RequireAchievementId = table.Column<int>(type: "int", nullable: false),
                    RequireAchievementRank = table.Column<int>(type: "int", nullable: false),
                    BannerName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PromoData = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RestockUnavailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequireMinLevel = table.Column<short>(type: "smallint", nullable: false),
                    RequireMaxLevel = table.Column<short>(type: "smallint", nullable: false),
                    PcCafe = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowSaleTime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PremiumMarketEntryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premium-market-entry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_premium-market-entry_premium-market-entry_PremiumMarketEntry~",
                        column: x => x.PremiumMarketEntryId,
                        principalTable: "premium-market-entry",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_premium-market-entry_PremiumMarketEntryId",
                table: "premium-market-entry",
                column: "PremiumMarketEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banner");

            migrationBuilder.DropTable(
                name: "premium-market-entry");
        }
    }
}
