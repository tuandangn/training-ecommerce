using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCostPriceToProductAndOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                schema: "tbl",
                table: "Product",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                schema: "tbl",
                table: "OrderItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "Order",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                schema: "tbl",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                schema: "tbl",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "tbl",
                table: "Order");
        }
    }
}
