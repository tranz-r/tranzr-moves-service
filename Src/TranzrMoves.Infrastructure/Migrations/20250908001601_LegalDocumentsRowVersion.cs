using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LegalDocumentsRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LegalDocuments");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "LegalDocuments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldRowVersion: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "LegalDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "LegalDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "xmin",
                table: "LegalDocuments",
                type: "text",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                table: "LegalDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
