using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOutQuoteV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerQuotes",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "DriverQuotes",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "InventoryItems",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "QuoteAdditionalPayments",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "Quotes",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "QuoteSessions",
                schema: "tranzrmoves");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteSessions",
                schema: "tranzrmoves",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ETag = table.Column<string>(type: "text", nullable: false),
                    ExpiresUtc = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: true),
                    SupabaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillingAddress_Accuracy = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_AddressNumber = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_City = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_Country = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_CountryCode = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_County = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_District = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_Floor = table.Column<int>(type: "integer", nullable: true),
                    BillingAddress_FullAddress = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    BillingAddress_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAddress_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    BillingAddress_Line1 = table.Column<string>(type: "text", nullable: false),
                    BillingAddress_Line2 = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    BillingAddress_MapboxId = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_Neighborhood = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_PlaceName = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_PostCode = table.Column<string>(type: "text", nullable: false),
                    BillingAddress_Region = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_RegionCode = table.Column<string>(type: "text", nullable: true),
                    BillingAddress_Street = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionDate = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    DeliveryDate = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    DistanceMiles = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverCount = table.Column<long>(type: "bigint", nullable: false),
                    DueDate = table.Column<LocalDate>(type: "date", nullable: true),
                    FlexibleTime = table.Column<bool>(type: "boolean", nullable: true),
                    Hours = table.Column<int>(type: "integer", nullable: true),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    NumberOfItemsToAssemble = table.Column<int>(type: "integer", nullable: false),
                    NumberOfItemsToDismantle = table.Column<int>(type: "integer", nullable: false),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    PaymentMethodId = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: true),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    PricingTier = table.Column<int>(type: "integer", nullable: true),
                    QuoteReference = table.Column<string>(type: "text", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    StripeSessionId = table.Column<string>(type: "text", nullable: true),
                    TimeSlot = table.Column<string>(type: "text", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    VanType = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Destination_Accuracy = table.Column<string>(type: "text", nullable: true),
                    Destination_AddressNumber = table.Column<string>(type: "text", nullable: true),
                    Destination_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destination_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destination_CountryCode = table.Column<string>(type: "text", nullable: true),
                    Destination_County = table.Column<string>(type: "text", nullable: true),
                    Destination_District = table.Column<string>(type: "text", nullable: true),
                    Destination_Floor = table.Column<int>(type: "integer", nullable: true),
                    Destination_FullAddress = table.Column<string>(type: "text", nullable: true),
                    Destination_HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    Destination_Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Destination_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Destination_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Destination_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Destination_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Destination_MapboxId = table.Column<string>(type: "text", nullable: true),
                    Destination_Neighborhood = table.Column<string>(type: "text", nullable: true),
                    Destination_PlaceName = table.Column<string>(type: "text", nullable: true),
                    Destination_PostCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Destination_Region = table.Column<string>(type: "text", nullable: true),
                    Destination_RegionCode = table.Column<string>(type: "text", nullable: true),
                    Destination_Street = table.Column<string>(type: "text", nullable: true),
                    Destination_UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin_Accuracy = table.Column<string>(type: "text", nullable: true),
                    Origin_AddressNumber = table.Column<string>(type: "text", nullable: true),
                    Origin_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Origin_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Origin_CountryCode = table.Column<string>(type: "text", nullable: true),
                    Origin_County = table.Column<string>(type: "text", nullable: true),
                    Origin_District = table.Column<string>(type: "text", nullable: true),
                    Origin_Floor = table.Column<int>(type: "integer", nullable: true),
                    Origin_FullAddress = table.Column<string>(type: "text", nullable: true),
                    Origin_HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    Origin_Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Origin_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Origin_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Origin_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Origin_MapboxId = table.Column<string>(type: "text", nullable: true),
                    Origin_Neighborhood = table.Column<string>(type: "text", nullable: true),
                    Origin_PlaceName = table.Column<string>(type: "text", nullable: true),
                    Origin_PostCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Origin_Region = table.Column<string>(type: "text", nullable: true),
                    Origin_RegionCode = table.Column<string>(type: "text", nullable: true),
                    Origin_Street = table.Column<string>(type: "text", nullable: true),
                    Origin_UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_QuoteSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuoteSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuotes",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuotes_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "tranzrmoves",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverQuotes",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverQuotes_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverQuotes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "tranzrmoves",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                schema: "tranzrmoves",
                columns: table => new
                {
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    Depth = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => new { x.QuoteId, x.Id });
                    table.ForeignKey(
                        name: "FK_InventoryItems_QuotesV2_QuoteId1",
                        column: x => x.QuoteId1,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuotesV2",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteAdditionalPayments",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    PaymentMethodId = table.Column<string>(type: "text", nullable: true),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteAdditionalPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteAdditionalPayments_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotes_QuoteId",
                schema: "tranzrmoves",
                table: "CustomerQuotes",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotes_UserId_QuoteId",
                schema: "tranzrmoves",
                table: "CustomerQuotes",
                columns: new[] { "UserId", "QuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverQuotes_QuoteId",
                schema: "tranzrmoves",
                table: "DriverQuotes",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverQuotes_UserId_QuoteId",
                schema: "tranzrmoves",
                table: "DriverQuotes",
                columns: new[] { "UserId", "QuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_QuoteId",
                schema: "tranzrmoves",
                table: "InventoryItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems",
                column: "QuoteId1");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAdditionalPayments_QuoteId",
                schema: "tranzrmoves",
                table: "QuoteAdditionalPayments",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedAt",
                schema: "tranzrmoves",
                table: "Quotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteReference",
                schema: "tranzrmoves",
                table: "Quotes",
                column: "QuoteReference");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SessionId",
                schema: "tranzrmoves",
                table: "Quotes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_StripeSessionId",
                schema: "tranzrmoves",
                table: "Quotes",
                column: "StripeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                schema: "tranzrmoves",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "tranzrmoves",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_CreatedAt",
                schema: "tranzrmoves",
                table: "Users",
                columns: new[] { "Role", "CreatedAt" });
        }
    }
}
