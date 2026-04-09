using Microsoft.EntityFrameworkCore.Migrations;
using TranzrMoves.Domain.Constants;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchAliasAndSearchText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "SearchAliases",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql($@"
        UPDATE ""{Db.SCHEMA}"".""{Db.Tables.InventoryGoods}""
        SET ""SearchText"" = lower(trim(""Name""));
    ");

            migrationBuilder.Sql($@"
        CREATE INDEX IF NOT EXISTS ""ix_inventory_goods_search_text_trgm""
        ON ""{Db.SCHEMA}"".""{Db.Tables.InventoryGoods}""
        USING gin (""SearchText"" gin_trgm_ops);
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchAliases",
                schema: "tranzrmoves",
                table: "InventoryGoods");

            migrationBuilder.DropColumn(
                name: "SearchText",
                schema: "tranzrmoves",
                table: "InventoryGoods");

            migrationBuilder.Sql($@"
        DROP INDEX IF EXISTS ""{Db.SCHEMA}"".""ix_inventory_goods_search_text_trgm"";
    ");
        }
    }
}
