using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NoCascadeDeleteSession_Quote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes");

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "Quotes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes",
                column: "SessionId",
                principalTable: "QuoteSessions",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Quotes");

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "Quotes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes",
                column: "SessionId",
                principalTable: "QuoteSessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
