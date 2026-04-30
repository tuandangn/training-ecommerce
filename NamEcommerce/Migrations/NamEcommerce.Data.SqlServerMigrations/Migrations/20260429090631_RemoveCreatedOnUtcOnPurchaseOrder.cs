using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreatedOnUtcOnPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "PurchaseOrderItem");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "PurchaseOrderItem",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "tbl",
                table: "PurchaseOrderItem");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "PurchaseOrderItem",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
