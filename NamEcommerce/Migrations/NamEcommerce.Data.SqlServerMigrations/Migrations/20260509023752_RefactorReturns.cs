using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RefactorReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerReturn_OrderId_Status",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                schema: "tbl",
                table: "VendorReturnItem");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                schema: "tbl",
                table: "CustomerReturnItem");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.DropColumn(
                name: "OrderId",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitCost",
                schema: "tbl",
                table: "VendorReturnItem",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnUnitCost",
                schema: "tbl",
                table: "VendorReturnItem",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalCost",
                schema: "tbl",
                table: "VendorReturn",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                schema: "tbl",
                table: "CustomerReturnItem",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnUnitPrice",
                schema: "tbl",
                table: "CustomerReturnItem",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalCost",
                schema: "tbl",
                table: "CustomerReturn",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNoteCode",
                schema: "tbl",
                table: "CustomerReturn",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryNoteId",
                schema: "tbl",
                table: "CustomerReturn",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_DeliveryNoteId_Status",
                schema: "tbl",
                table: "CustomerReturn",
                columns: new[] { "DeliveryNoteId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerReturn_DeliveryNoteId_Status",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.DropColumn(
                name: "OriginalUnitCost",
                schema: "tbl",
                table: "VendorReturnItem");

            migrationBuilder.DropColumn(
                name: "ReturnUnitCost",
                schema: "tbl",
                table: "VendorReturnItem");

            migrationBuilder.DropColumn(
                name: "AdditionalCost",
                schema: "tbl",
                table: "VendorReturn");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                schema: "tbl",
                table: "CustomerReturnItem");

            migrationBuilder.DropColumn(
                name: "ReturnUnitPrice",
                schema: "tbl",
                table: "CustomerReturnItem");

            migrationBuilder.DropColumn(
                name: "AdditionalCost",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.DropColumn(
                name: "DeliveryNoteCode",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.DropColumn(
                name: "DeliveryNoteId",
                schema: "tbl",
                table: "CustomerReturn");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                schema: "tbl",
                table: "VendorReturnItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                schema: "tbl",
                table: "CustomerReturnItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OrderCode",
                schema: "tbl",
                table: "CustomerReturn",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                schema: "tbl",
                table: "CustomerReturn",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_OrderId_Status",
                schema: "tbl",
                table: "CustomerReturn",
                columns: new[] { "OrderId", "Status" });
        }
    }
}
