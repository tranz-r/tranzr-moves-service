using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TranzrMovesDbSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "ServiceFeatures",
                newName: "ServiceFeatures",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "RateCards",
                newName: "RateCards",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "QuoteSessions",
                newName: "QuoteSessions",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "Quotes",
                newName: "Quotes",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "QuoteAdditionalPayments",
                newName: "QuoteAdditionalPayments",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "LegalDocuments",
                newName: "LegalDocuments",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "InventoryItems",
                newName: "InventoryItems",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "DriverQuotes",
                newName: "DriverQuotes",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "CustomerQuotes",
                newName: "CustomerQuotes",
                newSchema: "tranzrmoves");

            migrationBuilder.RenameTable(
                name: "AdditionalPrices",
                newName: "AdditionalPrices",
                newSchema: "tranzrmoves");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                schema: "tranzrmoves",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "ServiceFeatures",
                schema: "tranzrmoves",
                newName: "ServiceFeatures");

            migrationBuilder.RenameTable(
                name: "RateCards",
                schema: "tranzrmoves",
                newName: "RateCards");

            migrationBuilder.RenameTable(
                name: "QuoteSessions",
                schema: "tranzrmoves",
                newName: "QuoteSessions");

            migrationBuilder.RenameTable(
                name: "Quotes",
                schema: "tranzrmoves",
                newName: "Quotes");

            migrationBuilder.RenameTable(
                name: "QuoteAdditionalPayments",
                schema: "tranzrmoves",
                newName: "QuoteAdditionalPayments");

            migrationBuilder.RenameTable(
                name: "LegalDocuments",
                schema: "tranzrmoves",
                newName: "LegalDocuments");

            migrationBuilder.RenameTable(
                name: "InventoryItems",
                schema: "tranzrmoves",
                newName: "InventoryItems");

            migrationBuilder.RenameTable(
                name: "DriverQuotes",
                schema: "tranzrmoves",
                newName: "DriverQuotes");

            migrationBuilder.RenameTable(
                name: "CustomerQuotes",
                schema: "tranzrmoves",
                newName: "CustomerQuotes");

            migrationBuilder.RenameTable(
                name: "AdditionalPrices",
                schema: "tranzrmoves",
                newName: "AdditionalPrices");
        }
    }
}
