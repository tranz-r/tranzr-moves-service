using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranzrMoves.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedMapBoxAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Accuracy",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_AddressNumber",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_CountryCode",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_District",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BillingAddress_Latitude",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BillingAddress_Longitude",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_MapboxId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Neighborhood",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_PlaceName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Region",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_RegionCode",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Street",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_Accuracy",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_AddressNumber",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_CountryCode",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_District",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Destination_Latitude",
                table: "Quotes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Destination_Longitude",
                table: "Quotes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_MapboxId",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_Neighborhood",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_PlaceName",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_Region",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_RegionCode",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination_Street",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_Accuracy",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_AddressNumber",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_CountryCode",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_District",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Origin_Latitude",
                table: "Quotes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Origin_Longitude",
                table: "Quotes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_MapboxId",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_Neighborhood",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_PlaceName",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_Region",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_RegionCode",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin_Street",
                table: "Quotes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress_Accuracy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_AddressNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_CountryCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_District",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Latitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Longitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_MapboxId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Neighborhood",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_PlaceName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Region",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_RegionCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Street",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Destination_Accuracy",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_AddressNumber",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_CountryCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_District",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_Latitude",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_Longitude",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_MapboxId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_Neighborhood",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_PlaceName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_Region",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_RegionCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Destination_Street",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_Accuracy",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_AddressNumber",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_CountryCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_District",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_Latitude",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_Longitude",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_MapboxId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_Neighborhood",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_PlaceName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_Region",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_RegionCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Origin_Street",
                table: "Quotes");
        }
    }
}
