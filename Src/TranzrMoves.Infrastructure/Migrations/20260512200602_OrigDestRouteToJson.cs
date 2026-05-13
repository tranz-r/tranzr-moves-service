using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrigDestRouteToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE tranzrmoves."QuotesV2"
                    ALTER COLUMN "OriginDestinationRoute"
                    TYPE jsonb
                    USING "OriginDestinationRoute"::jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OriginDestinationRoute",
                schema: "tranzrmoves",
                table: "QuotesV2",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
