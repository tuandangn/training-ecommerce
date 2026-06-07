using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260607110000_AddDeliveryCompletionMetadata")]
    public partial class AddDeliveryCompletionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryCompletionIdempotencyKey",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCompletionNote",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCompletionSource",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryLatitude",
                schema: "tbl",
                table: "DeliveryNote",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryLocationAddress",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryLongitude",
                schema: "tbl",
                table: "DeliveryNote",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNote_DeliveryCompletionIdempotencyKey",
                schema: "tbl",
                table: "DeliveryNote",
                column: "DeliveryCompletionIdempotencyKey",
                unique: true,
                filter: "[DeliveryCompletionIdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryNote_DeliveryCompletionIdempotencyKey",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryCompletionIdempotencyKey",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryCompletionNote",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryCompletionSource",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryLatitude",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryLocationAddress",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryLongitude",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
