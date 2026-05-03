using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserV2ProfileAddressesOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuotesV2_UsersV2_UserId",
                schema: "tranzrmoves",
                table: "QuotesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersV2_AddressesV2_BillingAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersV2_AddressesV2_CommercialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersV2_AddressesV2_ResidentialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.Sql(
                """
                UPDATE tranzrmoves."AddressesV2" AS a
                SET "UserId" = u."Id", "Type" = 'Billing'
                FROM tranzrmoves."UsersV2" AS u
                WHERE u."BillingAddressId" IS NOT NULL AND u."BillingAddressId" = a."Id";

                UPDATE tranzrmoves."AddressesV2" AS a
                SET "UserId" = u."Id", "Type" = 'Residential'
                FROM tranzrmoves."UsersV2" AS u
                WHERE u."ResidentialAddressId" IS NOT NULL AND u."ResidentialAddressId" = a."Id";

                UPDATE tranzrmoves."AddressesV2" AS a
                SET "UserId" = u."Id", "Type" = 'Commercial'
                FROM tranzrmoves."UsersV2" AS u
                WHERE u."CommercialAddressId" IS NOT NULL AND u."CommercialAddressId" = a."Id";
                """);

            migrationBuilder.DropIndex(
                name: "IX_UsersV2_BillingAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropIndex(
                name: "IX_UsersV2_CommercialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropIndex(
                name: "IX_UsersV2_Email",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropIndex(
                name: "IX_UsersV2_ResidentialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropIndex(
                name: "IX_AddressesV2_UserId",
                schema: "tranzrmoves",
                table: "AddressesV2");

            migrationBuilder.DropColumn(
                name: "BillingAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropColumn(
                name: "CommercialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropColumn(
                name: "ResidentialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "tranzrmoves",
                table: "QuotesV2",
                newName: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersV2_Email",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddressesV2_UserId_Type",
                schema: "tranzrmoves",
                table: "AddressesV2",
                columns: new[] { "UserId", "Type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AddressesV2_UsersV2_UserId",
                schema: "tranzrmoves",
                table: "AddressesV2",
                column: "UserId",
                principalSchema: "tranzrmoves",
                principalTable: "UsersV2",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuotesV2_UsersV2_CustomerId",
                schema: "tranzrmoves",
                table: "QuotesV2",
                column: "CustomerId",
                principalSchema: "tranzrmoves",
                principalTable: "UsersV2",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AddressesV2_UsersV2_UserId",
                schema: "tranzrmoves",
                table: "AddressesV2");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotesV2_UsersV2_CustomerId",
                schema: "tranzrmoves",
                table: "QuotesV2");

            migrationBuilder.DropIndex(
                name: "IX_UsersV2_Email",
                schema: "tranzrmoves",
                table: "UsersV2");

            migrationBuilder.DropIndex(
                name: "IX_AddressesV2_UserId_Type",
                schema: "tranzrmoves",
                table: "AddressesV2");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                schema: "tranzrmoves",
                table: "QuotesV2",
                newName: "UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "BillingAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommercialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResidentialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "tranzrmoves",
                table: "AddressesV2",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

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

            migrationBuilder.CreateIndex(
                name: "IX_AddressesV2_UserId",
                schema: "tranzrmoves",
                table: "AddressesV2",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotesV2_UsersV2_UserId",
                schema: "tranzrmoves",
                table: "QuotesV2",
                column: "UserId",
                principalSchema: "tranzrmoves",
                principalTable: "UsersV2",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersV2_AddressesV2_BillingAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "BillingAddressId",
                principalSchema: "tranzrmoves",
                principalTable: "AddressesV2",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersV2_AddressesV2_CommercialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "CommercialAddressId",
                principalSchema: "tranzrmoves",
                principalTable: "AddressesV2",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersV2_AddressesV2_ResidentialAddressId",
                schema: "tranzrmoves",
                table: "UsersV2",
                column: "ResidentialAddressId",
                principalSchema: "tranzrmoves",
                principalTable: "AddressesV2",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
