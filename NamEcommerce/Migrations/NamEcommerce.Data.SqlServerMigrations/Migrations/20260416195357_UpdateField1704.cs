using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateField1704 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Warehouse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Warehouse",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Vendor",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Vendor",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "UnitMeasurement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "UnitMeasurement",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "StockMovementLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "StockMovementLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Role",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Role",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCollectMoney",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Product",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Picture",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Picture",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Permission",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Permission",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Order",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "InventoryStock",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "InventoryStock",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Expenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "DeliveryNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "DeliveryNote",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNote",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Customer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Customer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Category",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tbl",
                table: "Category",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Vendor");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Vendor");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "UnitMeasurement");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "UnitMeasurement");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "StockMovementLog");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "StockMovementLog");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "IsCollectMoney",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "InventoryStock");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "InventoryStock");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tbl",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tbl",
                table: "Category");
        }
    }
}
