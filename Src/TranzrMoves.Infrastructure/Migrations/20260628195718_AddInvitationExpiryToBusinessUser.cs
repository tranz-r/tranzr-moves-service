using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationExpiryToBusinessUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "InvitationExpiresAt",
                schema: "tranzrmoves",
                table: "BusinessUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitationExpiresAt",
                schema: "tranzrmoves",
                table: "BusinessUsers");
        }
    }
}
