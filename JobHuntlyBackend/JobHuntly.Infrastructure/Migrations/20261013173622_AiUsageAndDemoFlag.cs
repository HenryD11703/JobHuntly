using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobHuntly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AiUsageAndDemoFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AiUsage",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsage", x => new { x.UserId, x.Day });
                    table.ForeignKey(
                        name: "FK_AiUsage_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsage");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "Users");
        }
    }
}
