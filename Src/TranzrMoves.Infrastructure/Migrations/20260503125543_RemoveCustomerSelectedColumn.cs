using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerSelectedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerSelectedVans",
                schema: "tranzrmoves",
                table: "QuotesV2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerSelectedVans",
                schema: "tranzrmoves",
                table: "QuotesV2",
                type: "integer",
                nullable: true);
        }
    }
}
