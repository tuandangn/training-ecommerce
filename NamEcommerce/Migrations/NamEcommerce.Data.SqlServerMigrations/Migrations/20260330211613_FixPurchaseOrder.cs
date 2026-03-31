using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class FixPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedOnUtc",
                schema: "tbl",
                table: "PurchaseOrderItem");

            migrationBuilder.RenameColumn(
                name: "ExpectedDeliveryDate",
                schema: "tbl",
                table: "PurchaseOrder",
                newName: "ExpectedDeliveryDateUtc");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpectedDeliveryDateUtc",
                schema: "tbl",
                table: "PurchaseOrder",
                newName: "ExpectedDeliveryDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOnUtc",
                schema: "tbl",
                table: "PurchaseOrderItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

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
        }
    }
}
