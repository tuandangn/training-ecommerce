using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderBusinessWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidOnUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                schema: "tbl",
                table: "Order",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNote",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedOnUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingNote",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaidOnUtc",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaymentNote",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippedOnUtc",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippingNote",
                schema: "tbl",
                table: "Order");
        }
    }
}
