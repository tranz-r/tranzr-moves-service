using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStepsDirtyToQuoteV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StepsDirty",
                schema: "tranzrmoves",
                table: "QuotesV2",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StepsDirty",
                schema: "tranzrmoves",
                table: "QuotesV2");
        }
    }
}
