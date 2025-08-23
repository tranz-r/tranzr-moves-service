using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuoteCookieRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Jobs_JobId",
                table: "InventoryItems");

            migrationBuilder.DropTable(
                name: "CustomerJobs");

            migrationBuilder.DropTable(
                name: "DriverJobs");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_AddressLine2",
                table: "Users",
                newName: "BillingAddress_Line2");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_AddressLine1",
                table: "Users",
                newName: "BillingAddress_Line1");

            migrationBuilder.AddColumn<int>(
                name: "BillingAddress_Floor",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "BillingAddress_HasElevator",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "QuoteSessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    ETag = table.Column<string>(type: "text", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Origin_Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin_UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Origin_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Origin_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Origin_County = table.Column<string>(type: "text", nullable: true),
                    Origin_PostCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Origin_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Origin_HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    Origin_Floor = table.Column<int>(type: "integer", nullable: true),
                    Destination_Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Destination_UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Destination_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Destination_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Destination_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destination_County = table.Column<string>(type: "text", nullable: true),
                    Destination_PostCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Destination_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destination_HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    Destination_Floor = table.Column<int>(type: "integer", nullable: true),
                    DistanceMiles = table.Column<decimal>(type: "numeric", nullable: true),
                    NumberOfItemsToDismantle = table.Column<int>(type: "integer", nullable: false),
                    NumberOfItemsToAssemble = table.Column<int>(type: "integer", nullable: false),
                    VanType = table.Column<int>(type: "integer", nullable: false),
                    DriverCount = table.Column<long>(type: "bigint", nullable: false),
                    QuoteReference = table.Column<string>(type: "text", nullable: false),
                    CollectionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Hours = table.Column<int>(type: "integer", nullable: true),
                    FlexibleTime = table.Column<bool>(type: "boolean", nullable: true),
                    TimeSlot = table.Column<int>(type: "integer", nullable: true),
                    PricingTier = table.Column<int>(type: "integer", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: true),
                    PaymentType = table.Column<int>(type: "integer", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_QuoteSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "QuoteSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuotes_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverQuotes_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverQuotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotes_QuoteId",
                table: "CustomerQuotes",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotes_UserId_QuoteId",
                table: "CustomerQuotes",
                columns: new[] { "UserId", "QuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverQuotes_QuoteId",
                table: "DriverQuotes",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverQuotes_UserId_QuoteId",
                table: "DriverQuotes",
                columns: new[] { "UserId", "QuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SessionId",
                table: "Quotes",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Quotes_JobId",
                table: "InventoryItems",
                column: "JobId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Quotes_JobId",
                table: "InventoryItems");

            migrationBuilder.DropTable(
                name: "CustomerQuotes");

            migrationBuilder.DropTable(
                name: "DriverQuotes");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "QuoteSessions");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Floor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_HasElevator",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_Line2",
                table: "Users",
                newName: "BillingAddress_AddressLine2");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_Line1",
                table: "Users",
                newName: "BillingAddress_AddressLine1");

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    DistanceMiles = table.Column<long>(type: "bigint", nullable: false),
                    DriverCount = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    PricingTier = table.Column<int>(type: "integer", nullable: false),
                    QuoteId = table.Column<string>(type: "text", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true),
                    VanType = table.Column<int>(type: "integer", nullable: false),
                    Cost_BaseVan = table.Column<long>(type: "bigint", nullable: true),
                    Cost_Distance = table.Column<double>(type: "double precision", nullable: true),
                    Cost_Driver = table.Column<long>(type: "bigint", nullable: true),
                    Cost_ElevatorAdjustment = table.Column<long>(type: "bigint", nullable: true),
                    Cost_Floor = table.Column<long>(type: "bigint", nullable: true),
                    Cost_TierAdjustment = table.Column<double>(type: "double precision", nullable: true),
                    Cost_Total = table.Column<double>(type: "double precision", nullable: true),
                    Destination_AddressLine1 = table.Column<string>(type: "text", nullable: false),
                    Destination_AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    Destination_City = table.Column<string>(type: "text", nullable: true),
                    Destination_Country = table.Column<string>(type: "text", nullable: true),
                    Destination_County = table.Column<string>(type: "text", nullable: true),
                    Destination_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination_PostCode = table.Column<string>(type: "text", nullable: false),
                    Destination_UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin_AddressLine1 = table.Column<string>(type: "text", nullable: false),
                    Origin_AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    Origin_City = table.Column<string>(type: "text", nullable: true),
                    Origin_Country = table.Column<string>(type: "text", nullable: true),
                    Origin_County = table.Column<string>(type: "text", nullable: true),
                    Origin_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin_PostCode = table.Column<string>(type: "text", nullable: false),
                    Origin_UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerJobs_JobId",
                table: "CustomerJobs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerJobs_UserId_JobId",
                table: "CustomerJobs",
                columns: new[] { "UserId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverJobs_JobId",
                table: "DriverJobs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverJobs_UserId_JobId",
                table: "DriverJobs",
                columns: new[] { "UserId", "JobId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Jobs_JobId",
                table: "InventoryItems",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
