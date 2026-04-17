using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update17041 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCollectMoney",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "TrackInventory",
                schema: "tbl",
                table: "Product");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountToCollect",
                schema: "tbl",
                table: "DeliveryNote",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Surcharge",
                schema: "tbl",
                table: "DeliveryNote",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountToCollect",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "Surcharge",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.AddColumn<bool>(
                name: "IsCollectMoney",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrackInventory",
                schema: "tbl",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
