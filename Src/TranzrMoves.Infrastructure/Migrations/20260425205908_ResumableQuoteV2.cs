using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResumableQuoteV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AddressesV2",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullAddress = table.Column<string>(type: "text", nullable: true),
                    Line1 = table.Column<string>(type: "text", nullable: false),
                    Line2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: true),
                    PostCode = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    Floor = table.Column<int>(type: "integer", nullable: true),
                    AddressNumber = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Neighborhood = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    RegionCode = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    PlaceName = table.Column<string>(type: "text", nullable: true),
                    Accuracy = table.Column<string>(type: "text", nullable: true),
                    MapboxId = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressesV2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsersV2",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    BillingAddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResidentialAddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommercialAddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersV2_AddressesV2_BillingAddressId",
                        column: x => x.BillingAddressId,
                        principalSchema: "tranzrmoves",
                        principalTable: "AddressesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UsersV2_AddressesV2_CommercialAddressId",
                        column: x => x.CommercialAddressId,
                        principalSchema: "tranzrmoves",
                        principalTable: "AddressesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UsersV2_AddressesV2_ResidentialAddressId",
                        column: x => x.ResidentialAddressId,
                        principalSchema: "tranzrmoves",
                        principalTable: "AddressesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QuotesV2",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OriginToDestinationDistanceInMiles = table.Column<long>(type: "bigint", nullable: true),
                    BaseToOriginDistanceInMiles = table.Column<long>(type: "bigint", nullable: true),
                    OriginDestinationRoute = table.Column<string>(type: "text", nullable: true),
                    NumberOfItemsToDismantle = table.Column<int>(type: "integer", nullable: false),
                    NumberOfItemsToAssemble = table.Column<int>(type: "integer", nullable: false),
                    VanType = table.Column<string>(type: "text", nullable: false),
                    CrewCount = table.Column<long>(type: "bigint", nullable: false),
                    QuoteReference = table.Column<string>(type: "text", nullable: false),
                    TotalInventoryVolumeM3 = table.Column<decimal>(type: "numeric", nullable: true),
                    EffectiveVanCapacityM3 = table.Column<decimal>(type: "numeric", nullable: true),
                    RecommendedVanCount = table.Column<int>(type: "integer", nullable: true),
                    SelectedVanCount = table.Column<int>(type: "integer", nullable: true),
                    CustomerSelectedVans = table.Column<int>(type: "integer", nullable: true),
                    VanCapacityStatus = table.Column<string>(type: "text", nullable: true),
                    VanCapacityWarning = table.Column<string>(type: "text", nullable: true),
                    VanRecommendationCalculatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceTier = table.Column<string>(type: "text", nullable: true),
                    QuotePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    PriceCalculatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    StepsCompleted = table.Column<long>(type: "bigint", nullable: false),
                    LastCompletedStepKey = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    LastResumeEmailSentAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotesV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotesV2_UsersV2_UserId",
                        column: x => x.UserId,
                        principalSchema: "tranzrmoves",
                        principalTable: "UsersV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemsV2",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Depth = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemsV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemsV2_QuotesV2_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuotesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaymentMethodId = table.Column<string>(type: "text", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    SetupIntentId = table.Column<string>(type: "text", nullable: true),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<LocalDate>(type: "date", nullable: true),
                    StripeSessionId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_QuotesV2_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuotesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pricings",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteType = table.Column<string>(type: "text", nullable: false),
                    ServiceLevel = table.Column<string>(type: "text", nullable: false),
                    SelectedVanCount = table.Column<int>(type: "integer", nullable: false),
                    CrewCount = table.Column<int>(type: "integer", nullable: false),
                    BaseVanCost = table.Column<decimal>(type: "numeric", nullable: false),
                    DistanceCost = table.Column<decimal>(type: "numeric", nullable: false),
                    LabourCost = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseToOriginCost = table.Column<decimal>(type: "numeric", nullable: false),
                    NumberOfItemsToDismantle = table.Column<int>(type: "integer", nullable: false),
                    NumberOfItemsToAssemble = table.Column<int>(type: "integer", nullable: false),
                    DismantleCost = table.Column<decimal>(type: "numeric", nullable: false),
                    AssemblyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginFloorSurcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    DestinationFloorSurcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    SpecialHandlingCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CallOutCharge = table.Column<decimal>(type: "numeric", nullable: false),
                    UlezSurcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    CongestionZoneSurcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    InsuranceUplift = table.Column<decimal>(type: "numeric", nullable: false),
                    CongestionChargeApplies = table.Column<bool>(type: "boolean", nullable: false),
                    StandardBlockHours = table.Column<int>(type: "integer", nullable: false),
                    CostPerHourAfterStandard = table.Column<decimal>(type: "numeric", nullable: false),
                    Vat = table.Column<int>(type: "integer", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SubtotalWithoutVat = table.Column<decimal>(type: "numeric", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCostWithVat = table.Column<decimal>(type: "numeric", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pricings_QuotesV2_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuotesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteAddresses",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    FullAddress = table.Column<string>(type: "text", nullable: true),
                    Line1 = table.Column<string>(type: "text", nullable: false),
                    Line2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: true),
                    PostCode = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    Floor = table.Column<int>(type: "integer", nullable: true),
                    FreeParkingAccess = table.Column<bool>(type: "boolean", nullable: true),
                    PackingCost = table.Column<decimal>(type: "numeric", nullable: true),
                    AddressNumber = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Neighborhood = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    RegionCode = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    PlaceName = table.Column<string>(type: "text", nullable: true),
                    Accuracy = table.Column<string>(type: "text", nullable: true),
                    MapboxId = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteAddresses_QuotesV2_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuotesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionDate = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    Hours = table.Column<int>(type: "integer", nullable: true),
                    FlexibleTime = table.Column<bool>(type: "boolean", nullable: true),
                    TimeSlot = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_QuotesV2_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "tranzrmoves",
                        principalTable: "QuotesV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems",
                column: "QuoteId1");

            migrationBuilder.CreateIndex(
                name: "IX_AddressesV2_UserId",
                schema: "tranzrmoves",
                table: "AddressesV2",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemsV2_QuoteId",
                schema: "tranzrmoves",
                table: "InventoryItemsV2",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_QuoteId",
                schema: "tranzrmoves",
                table: "Payments",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pricings_QuoteId",
                schema: "tranzrmoves",
                table: "Pricings",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAddresses_QuoteId",
                schema: "tranzrmoves",
                table: "QuoteAddresses",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotesV2_CreatedAt",
                schema: "tranzrmoves",
                table: "QuotesV2",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuotesV2_QuoteReference",
                schema: "tranzrmoves",
                table: "QuotesV2",
                column: "QuoteReference");

            migrationBuilder.CreateIndex(
                name: "IX_QuotesV2_SessionId",
                schema: "tranzrmoves",
                table: "QuotesV2",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotesV2_UserId",
                schema: "tranzrmoves",
                table: "QuotesV2",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_QuoteId",
                schema: "tranzrmoves",
                table: "Schedules",
                column: "QuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersV2_BillingAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "BillingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersV2_CommercialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "CommercialAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersV2_Email",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UsersV2_ResidentialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "ResidentialAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_QuotesV2_QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems",
                column: "QuoteId1",
                principalSchema: "tranzrmoves",
                principalTable: "QuotesV2",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_QuotesV2_QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "InventoryItemsV2",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "Pricings",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "QuoteAddresses",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "Schedules",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "QuotesV2",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "UsersV2",
                schema: "tranzrmoves");

            migrationBuilder.DropTable(
                name: "AddressesV2",
                schema: "tranzrmoves");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "QuoteId1",
                schema: "tranzrmoves",
                table: "InventoryItems");
        }
    }
}
