using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_CreatedAt_PaymentStatus",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_PaymentStatus_Type",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_Type_CreatedAt",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteAdditionalPayments_QuoteId_Amount",
                table: "QuoteAdditionalPayments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedAt_PaymentStatus",
                table: "Quotes",
                columns: new[] { "CreatedAt", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_PaymentStatus_Type",
                table: "Quotes",
                columns: new[] { "PaymentStatus", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Type_CreatedAt",
                table: "Quotes",
                columns: new[] { "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAdditionalPayments_QuoteId_Amount",
                table: "QuoteAdditionalPayments",
                columns: new[] { "QuoteId", "Amount" });
        }
    }
}
