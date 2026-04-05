using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuoteCollectionDeliveryAsInstant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Instant>(
                name: "DeliveryDate",
                schema: "tranzrmoves",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(LocalDate),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "CollectionDate",
                schema: "tranzrmoves",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(LocalDate),
                oldType: "date",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<LocalDate>(
                name: "DeliveryDate",
                schema: "tranzrmoves",
                table: "Quotes",
                type: "date",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<LocalDate>(
                name: "CollectionDate",
                schema: "tranzrmoves",
                table: "Quotes",
                type: "date",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
