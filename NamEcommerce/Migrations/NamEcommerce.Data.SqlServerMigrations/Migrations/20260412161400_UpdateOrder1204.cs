using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrder1204 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "PaymentStatus",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippedOnUtc",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippingStatus",
                schema: "tbl",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "ShippingNote",
                schema: "tbl",
                table: "Order",
                newName: "LockOrderReason");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedShippingDateUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LockOrderReason",
                schema: "tbl",
                table: "Order",
                newName: "ShippingNote");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedShippingDateUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                schema: "tbl",
                table: "Order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedOnUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingStatus",
                schema: "tbl",
                table: "Order",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
