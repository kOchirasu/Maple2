using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maple2.Server.World.Migrations
{
    /// <inheritdoc />
    public partial class Shops2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisableInstantRestock",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "EnableRestockCostMultiplier",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "ExcessRestockCurrencyType",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "PersistantInventory",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "RestockCost",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "RestockCurrencyType",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "RestockInterval",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "RestockTime",
                table: "shop");

            migrationBuilder.DropColumn(
                name: "TotalRestockCount",
                table: "shop");

            migrationBuilder.RenameColumn(
                name: "StockPurchased",
                table: "shop-item",
                newName: "RequireGuildTrophy");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "shop-item",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RestockData",
                table: "shop",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "character-shop-data",
                columns: table => new
                {
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    RestockTime = table.Column<long>(type: "bigint", nullable: false),
                    RestockCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character-shop-data", x => new { x.ShopId, x.OwnerId });
                    table.ForeignKey(
                        name: "FK_character-shop-data_shop_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "character-shop-item-data",
                columns: table => new
                {
                    ShopItemId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    StockPurchased = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character-shop-item-data", x => new { x.ShopItemId, x.OwnerId });
                    table.ForeignKey(
                        name: "FK_character-shop-item-data_shop-item_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "shop-item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_character-shop-item-data_shop_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_character-shop-item-data_ShopId",
                table: "character-shop-item-data",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character-shop-data");

            migrationBuilder.DropTable(
                name: "character-shop-item-data");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "shop-item");

            migrationBuilder.DropColumn(
                name: "RestockData",
                table: "shop");

            migrationBuilder.RenameColumn(
                name: "RequireGuildTrophy",
                table: "shop-item",
                newName: "StockPurchased");

            migrationBuilder.AddColumn<bool>(
                name: "DisableInstantRestock",
                table: "shop",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableRestockCostMultiplier",
                table: "shop",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "ExcessRestockCurrencyType",
                table: "shop",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "PersistantInventory",
                table: "shop",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RestockCost",
                table: "shop",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "RestockCurrencyType",
                table: "shop",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "RestockInterval",
                table: "shop",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "RestockTime",
                table: "shop",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TotalRestockCount",
                table: "shop",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
