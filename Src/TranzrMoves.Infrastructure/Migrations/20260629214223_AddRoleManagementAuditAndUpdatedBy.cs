using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleManagementAuditAndUpdatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByBusinessUserId",
                schema: "tranzrmoves",
                table: "BusinessUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessUserRoleChanges",
                schema: "tranzrmoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetBusinessUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByBusinessUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromRole = table.Column<string>(type: "text", nullable: false),
                    ToRole = table.Column<string>(type: "text", nullable: false),
                    ChangeType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUserRoleChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUserRoleChanges_BusinessAccountId",
                schema: "tranzrmoves",
                table: "BusinessUserRoleChanges",
                column: "BusinessAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUserRoleChanges_TargetBusinessUserId",
                schema: "tranzrmoves",
                table: "BusinessUserRoleChanges",
                column: "TargetBusinessUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessUserRoleChanges",
                schema: "tranzrmoves");

            migrationBuilder.DropColumn(
                name: "UpdatedByBusinessUserId",
                schema: "tranzrmoves",
                table: "BusinessUsers");
        }
    }
}
