using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalPaymentToCorrectSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 ALTER TABLE IF EXISTS public."QuoteAdditionalPayments" SET SCHEMA tranzrmoves;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 ALTER TABLE IF EXISTS tranzrmoves."QuoteAdditionalPayments" SET SCHEMA public;
                                 """);
        }
    }
}
