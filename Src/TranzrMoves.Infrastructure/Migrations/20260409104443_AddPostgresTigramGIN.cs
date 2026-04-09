using Microsoft.EntityFrameworkCore.Migrations;
using TranzrMoves.Domain.Constants;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgresTigramGIN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            migrationBuilder.Sql($"""
                                      CREATE INDEX IF NOT EXISTS "ix_inventory_goods_name_trgm"
                                      ON "{Db.SCHEMA}"."{Db.Tables.InventoryGoods}"
                                      USING gin ("Name" gin_trgm_ops);
                                  """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                                      DROP INDEX IF EXISTS "{Db.SCHEMA}"."ix_inventory_goods_name_trgm";
                                  """);
        }
    }
}
