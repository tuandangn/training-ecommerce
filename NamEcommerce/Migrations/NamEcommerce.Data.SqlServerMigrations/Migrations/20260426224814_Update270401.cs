using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update270401 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseOrderId",
                schema: "tbl",
                table: "VendorDebt",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "PurchaseOrderCode",
                schema: "tbl",
                table: "VendorDebt",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptId",
                schema: "tbl",
                table: "VendorDebt",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCost",
                schema: "tbl",
                table: "InventoryStock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VendorAddress",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorPhone",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_GoodsReceiptId",
                schema: "tbl",
                table: "VendorDebt",
                column: "GoodsReceiptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorDebt_GoodsReceiptId",
                schema: "tbl",
                table: "VendorDebt");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptId",
                schema: "tbl",
                table: "VendorDebt");

            migrationBuilder.DropColumn(
                name: "AverageCost",
                schema: "tbl",
                table: "InventoryStock");

            migrationBuilder.DropColumn(
                name: "VendorAddress",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "VendorId",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "VendorName",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "VendorPhone",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseOrderId",
                schema: "tbl",
                table: "VendorDebt",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PurchaseOrderCode",
                schema: "tbl",
                table: "VendorDebt",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
