using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maple2.Server.World.Migrations
{
    public partial class PremiumClub : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumExpiration",
                table: "character-unlock",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PremiumRewardsClaimed",
                table: "character-config",
                type: "json",
                nullable: true,
                defaultValue: "[]")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremiumExpiration",
                table: "character-unlock");

            migrationBuilder.DropColumn(
                name: "PremiumRewardsClaimed",
                table: "character-config");
        }
    }
}
