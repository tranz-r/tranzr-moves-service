using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupabaseIdToUserV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupabaseId",
                schema: "tranzrmoves",
                table: "UsersV2",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersV2_SupabaseId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "SupabaseId",
                unique: true,
                filter: "\"SupabaseId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsersV2_SupabaseId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropColumn(
                name: "SupabaseId",
                schema: "tranzrmoves",
                table: "UsersV2");
        }
    }
}
