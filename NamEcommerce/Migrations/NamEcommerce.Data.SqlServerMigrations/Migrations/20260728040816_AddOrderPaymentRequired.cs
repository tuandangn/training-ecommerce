using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                schema: "tbl",
                table: "Order",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentIntentId",
                schema: "tbl",
                table: "Order",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProcessRequiresPayment",
                schema: "tbl",
                table: "Order",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidOnUtc",
                schema: "tbl",
                table: "DeliveryNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPaymentToConfirm",
                schema: "tbl",
                table: "DeliveryNote",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ProcessRequiresPayment",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaidOnUtc",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "RequiresPaymentToConfirm",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
