using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryConfirmedAtToQuoteV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "SummaryConfirmedAt",
                schema: "tranzrmoves",
                table: "QuotesV2",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummaryConfirmedAt",
                schema: "tranzrmoves",
                table: "QuotesV2");
        }
    }
}
