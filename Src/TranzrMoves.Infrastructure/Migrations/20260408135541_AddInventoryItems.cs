using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryGoods",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    popularity_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    length_cm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    width_cm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    volume_m3 = table.Column<decimal>(type: "numeric(12,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryGoods", x => x.Id);
                    table.CheckConstraint("CK_inventory_goods_height_cm_positive", "height_cm > 0");
                    table.CheckConstraint("CK_inventory_goods_length_cm_positive", "length_cm > 0");
                    table.CheckConstraint("CK_inventory_goods_popularity_index_non_negative", "popularity_index >= 0");
                    table.CheckConstraint("CK_inventory_goods_volume_m3_positive", "volume_m3 > 0");
                    table.CheckConstraint("CK_inventory_goods_width_cm_positive", "width_cm > 0");
                    table.ForeignKey(
                        name: "FK_InventoryGoods_InventoryCategories_category_id",
                        column: x => x.category_id,
                        principalSchema: "tranzrmoves",
                        principalTable: "InventoryCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_Name",
                schema: "tranzrmoves",
                table: "InventoryCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryGoods_category_id",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryGoods_category_id_name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                columns: new[] { "category_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryGoods_name",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryGoods_popularity_index",
                schema: "tranzrmoves",
                table: "InventoryGoods",
                column: "popularity_index");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryGoods",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "InventoryCategories",
                schema: "tranzrmoves");
        }
    }
}
