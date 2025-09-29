using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixInventoryItemsForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Quotes_JobId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "InventoryItems",
                newName: "QuoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Quotes_QuoteId",
                table: "InventoryItems",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Quotes_QuoteId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "QuoteId",
                table: "InventoryItems",
                newName: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Quotes_JobId",
                table: "InventoryItems",
                column: "JobId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
