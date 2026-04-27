using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class SetGoodsReceiptToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderCode",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseOrderCode",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
