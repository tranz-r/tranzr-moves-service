using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAccountAndBusinessUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessAccounts",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BusinessEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    BusinessPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyRegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BillingAddress_FullAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BillingAddress_Line1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BillingAddress_Line2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BillingAddress_City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_County = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_PostCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BillingAddress_Country = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    BillingAddress_Floor = table.Column<int>(type: "integer", nullable: true),
                    BillingAddress_AddressNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingAddress_Street = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BillingAddress_Neighborhood = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_District = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_Region = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_RegionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingAddress_CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BillingAddress_PlaceName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BillingAddress_Accuracy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingAddress_MapboxId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingAddress_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    BillingAddress_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUsers",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessUsers_BusinessAccounts_BusinessAccountId",
                        column: x => x.BusinessAccountId,
                        principalSchema: "tranzrmoves",
                        principalTable: "BusinessAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessUsers_UsersV2_UserId",
                        column: x => x.UserId,
                        principalSchema: "tranzrmoves",
                        principalTable: "UsersV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAccounts_BusinessEmail",
                schema: "tranzrmoves",
                table: "BusinessAccounts",
                column: "BusinessEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUsers_BusinessAccountId",
                schema: "tranzrmoves",
                table: "BusinessUsers",
                column: "BusinessAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUsers_UserId",
                schema: "tranzrmoves",
                table: "BusinessUsers",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessUsers",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "BusinessAccounts",
                schema: "tranzrmoves");
        }
    }
}
