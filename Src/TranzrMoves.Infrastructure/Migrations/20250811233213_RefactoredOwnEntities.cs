using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredOwnEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Addresses_DestinationId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Addresses_OriginId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_PricingTiers_PricingTierId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "PricingTiers");

            migrationBuilder.DropTable(
                name: "UserJobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_DestinationId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_OriginId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_PricingTierId",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_JobId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Users",
                newName: "BillingAddress_County");

            migrationBuilder.RenameColumn(
                name: "PricingTierId",
                table: "Jobs",
                newName: "Origin_UserId");

            migrationBuilder.RenameColumn(
                name: "OriginId",
                table: "Jobs",
                newName: "Origin_Id");

            migrationBuilder.RenameColumn(
                name: "DestinationId",
                table: "Jobs",
                newName: "Destination_UserId");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_AddressLine1",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_AddressLine2",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_City",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Country",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingAddress_Id",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_PostCode",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CollectionDate",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "Cost_BaseVan",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cost_Distance",
                table: "Jobs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Cost_Driver",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Cost_ElevatorAdjustment",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Cost_Floor",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cost_TierAdjustment",
                table: "Jobs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cost_Total",
                table: "Jobs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_AddressLine1",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Destination_AddressLine2",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_City",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Destination_Country",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_County",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Destination_Id",
                table: "Jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Destination_PostCode",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "DistanceMiles",
                table: "Jobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DriverCount",
                table: "Jobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Origin_AddressLine1",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origin_AddressLine2",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_City",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origin_Country",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_County",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_PostCode",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PricingTier",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VanType",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems",
                columns: new[] { "JobId", "Id" });

            migrationBuilder.CreateTable(
                name: "CustomerJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerJobs");

            migrationBuilder.DropTable(
                name: "DriverJobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "BillingAddress_AddressLine1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_AddressLine2",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_City",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_PostCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CollectionDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_BaseVan",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_Distance",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_Driver",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_ElevatorAdjustment",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_Floor",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_TierAdjustment",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Cost_Total",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_AddressLine1",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_AddressLine2",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_City",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_Country",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_County",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_Id",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Destination_PostCode",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DistanceMiles",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DriverCount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Origin_AddressLine1",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Origin_AddressLine2",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Origin_City",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Origin_Country",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Origin_County",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Origin_PostCode",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PricingTier",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "VanType",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Users",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_County",
                table: "Users",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Origin_UserId",
                table: "Jobs",
                newName: "PricingTierId");

            migrationBuilder.RenameColumn(
                name: "Origin_Id",
                table: "Jobs",
                newName: "OriginId");

            migrationBuilder.RenameColumn(
                name: "Destination_UserId",
                table: "Jobs",
                newName: "DestinationId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressLine1 = table.Column<string>(type: "text", nullable: false),
                    AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    PostCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserJobs",
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
                    table.PrimaryKey("PK_UserJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DestinationId",
                table: "Jobs",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_OriginId",
                table: "Jobs",
                column: "OriginId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_PricingTierId",
                table: "Jobs",
                column: "PricingTierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_JobId",
                table: "InventoryItems",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId",
                table: "Addresses",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserJobs_JobId",
                table: "UserJobs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJobs_UserId",
                table: "UserJobs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Addresses_DestinationId",
                table: "Jobs",
                column: "DestinationId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Addresses_OriginId",
                table: "Jobs",
                column: "OriginId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_PricingTiers_PricingTierId",
                table: "Jobs",
                column: "PricingTierId",
                principalTable: "PricingTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
