using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseOrderWF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedOnUtc",
                schema: "tbl",
                table: "VendorReturn",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversedReason",
                schema: "tbl",
                table: "VendorReturn",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedShippingAmount",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedTaxAmount",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_Code",
                schema: "tbl",
                table: "GoodsReceipt",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceCustomerReturnId",
                unique: true,
                filter: "[SourceCustomerReturnId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceVendorReturnId",
                unique: true,
                filter: "[SourceVendorReturnId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipt_Code",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ReversedOnUtc",
                schema: "tbl",
                table: "VendorReturn");

            migrationBuilder.DropColumn(
                name: "ReversedReason",
                schema: "tbl",
                table: "VendorReturn");

            migrationBuilder.DropColumn(
                name: "AccumulatedShippingAmount",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "AccumulatedTaxAmount",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "ClosedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses");
        }
    }
}
