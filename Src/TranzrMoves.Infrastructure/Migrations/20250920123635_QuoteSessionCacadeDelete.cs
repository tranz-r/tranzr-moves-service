using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuoteSessionCacadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes",
                column: "SessionId",
                principalTable: "QuoteSessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_QuoteSessions_SessionId",
                table: "Quotes",
                column: "SessionId",
                principalTable: "QuoteSessions",
                principalColumn: "SessionId");
        }
    }
}
