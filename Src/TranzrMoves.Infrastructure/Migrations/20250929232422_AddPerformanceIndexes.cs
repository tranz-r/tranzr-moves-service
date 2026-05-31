using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_CreatedAt",
                table: "Users",
                columns: new[] { "Role", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedAt",
                table: "Quotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedAt_PaymentStatus",
                table: "Quotes",
                columns: new[] { "CreatedAt", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_PaymentStatus_Type",
                table: "Quotes",
                columns: new[] { "PaymentStatus", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteReference",
                table: "Quotes",
                column: "QuoteReference");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_StripeSessionId",
                table: "Quotes",
                column: "StripeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Type_CreatedAt",
                table: "Quotes",
                columns: new[] { "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAdditionalPayments_QuoteId_Amount",
                table: "QuoteAdditionalPayments",
                columns: new[] { "QuoteId", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_QuoteId",
                table: "InventoryItems",
                column: "QuoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_CreatedAt",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_CreatedAt_PaymentStatus",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_PaymentStatus_Type",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_QuoteReference",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_StripeSessionId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_Type_CreatedAt",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteAdditionalPayments_QuoteId_Amount",
                table: "QuoteAdditionalPayments");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_QuoteId",
                table: "InventoryItems");
        }
    }
}
