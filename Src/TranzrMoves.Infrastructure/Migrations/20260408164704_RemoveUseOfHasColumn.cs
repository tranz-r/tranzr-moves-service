using Microsoft.EntityFrameworkCore.Migrations;
using TranzrMoves.Domain.Constants;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUseOfHasColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryGoods_InventoryCategories_category_id",
                schema: "tranzrmoves",
                table: "InventoryGoods");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "width_cm",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "WidthCm");

            migrationBuilder.RenameColumn(
                name: "volume_m3",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "VolumeM3");

            migrationBuilder.RenameColumn(
                name: "popularity_index",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "PopularityIndex");

            migrationBuilder.RenameColumn(
                name: "length_cm",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "LengthCm");

            migrationBuilder.RenameColumn(
                name: "height_cm",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "HeightCm");

            migrationBuilder.RenameColumn(
                name: "category_id",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_Name");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_popularity_index",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_PopularityIndex");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_category_id_name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_CategoryId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_category_id",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryGoods_InventoryCategories_CategoryId",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                column: "CategoryId",
                principalSchema: "tranzrmoves",
                principalTable: "InventoryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql($"""
                                      CREATE INDEX IF NOT EXISTS "ix_inventory_good_name_lower_pattern"
                                      ON "{Db.SCHEMA}"."{Db.Tables.InventoryGoods}" (lower("Name") text_pattern_ops);
                                  """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryGoods_InventoryCategories_CategoryId",
                schema: "tranzrmoves",
                table: "InventoryGoods");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "WidthCm",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "width_cm");

            migrationBuilder.RenameColumn(
                name: "VolumeM3",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "volume_m3");

            migrationBuilder.RenameColumn(
                name: "PopularityIndex",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "popularity_index");

            migrationBuilder.RenameColumn(
                name: "LengthCm",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "length_cm");

            migrationBuilder.RenameColumn(
                name: "HeightCm",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "height_cm");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_Name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_name");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_PopularityIndex",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_popularity_index");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_CategoryId_Name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_category_id_name");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryGoods_CategoryId",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                newName: "IX_InventoryGoods_category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryGoods_InventoryCategories_category_id",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                column: "category_id",
                principalSchema: "tranzrmoves",
                principalTable: "InventoryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql($"""
                                      DROP INDEX IF EXISTS "{Db.SCHEMA}"."ix_inventory_good_name_lower_pattern";
                                  """);
        }
    }
}
