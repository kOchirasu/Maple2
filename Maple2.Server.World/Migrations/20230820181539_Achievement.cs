﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maple2.Server.World.Migrations
{
    /// <inheritdoc />
    public partial class Achievement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Trophy",
                table: "Account");

            migrationBuilder.AddColumn<string>(
                name: "InteractedObjects",
                table: "character-unlock",
                type: "json",
                defaultValue: "[]",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ItemCollects",
                table: "character-unlock",
                type: "json",
                defaultValue: "{}",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Achievement",
                columns: table => new
                {
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    CurrentGrade = table.Column<int>(type: "int", nullable: false),
                    RewardGrade = table.Column<int>(type: "int", nullable: false),
                    Favorite = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Counter = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Grades = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievement", x => new { x.AccountId, x.CharacterId, x.Id });
                    table.ForeignKey(
                        name: "FK_Achievement_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Achievement_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Achievement_CharacterId",
                table: "Achievement",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievement");

            migrationBuilder.DropColumn(
                name: "InteractedObjects",
                table: "character-unlock");

            migrationBuilder.DropColumn(
                name: "ItemCollects",
                table: "character-unlock");

            migrationBuilder.AddColumn<string>(
                name: "Trophy",
                table: "Account",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
