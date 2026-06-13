using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMarketingConsentsWithV3Preferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingConsents",
                schema: "notifications");

            migrationBuilder.CreateTable(
                name: "CustomerMarketingPreferences",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailMarketingEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SmsMarketingEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailMarketingConsentedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    SmsMarketingConsentedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMarketingPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingConsentEvents",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerMarketingPreferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingConsentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingConsentEvents_CustomerMarketingPreferences_Custome~",
                        column: x => x.CustomerMarketingPreferenceId,
                        principalSchema: "notifications",
                        principalTable: "CustomerMarketingPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMarketingPreferences_Email",
                schema: "notifications",
                table: "CustomerMarketingPreferences",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingConsentEvents_CustomerMarketingPreferenceId",
                schema: "notifications",
                table: "MarketingConsentEvents",
                column: "CustomerMarketingPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingConsentEvents_OccurredAt",
                schema: "notifications",
                table: "MarketingConsentEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingConsentEvents",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "CustomerMarketingPreferences",
                schema: "notifications");

            migrationBuilder.CreateTable(
                name: "MarketingConsents",
                schema: "notifications",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    IsOptedIn = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingConsents", x => x.Email);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingConsents_IsOptedIn",
                schema: "notifications",
                table: "MarketingConsents",
                column: "IsOptedIn");
        }
    }
}
