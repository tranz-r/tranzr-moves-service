using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.AddColumn<bool>(
                name: "CustomerSelectedOption",
                schema: "tranzrmoves",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerSelectedOption",
                schema: "tranzrmoves",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                schema: "tranzrmoves",
                table: "UsersV2",
                type: "text",
                nullable: true);
        }
    }
}
