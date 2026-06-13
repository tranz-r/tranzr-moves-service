using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingConsents",
                schema: "notifications",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    IsOptedIn = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingConsents",
                schema: "notifications");
        }
    }
}
