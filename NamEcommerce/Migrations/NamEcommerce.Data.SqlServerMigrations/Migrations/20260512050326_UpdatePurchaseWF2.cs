using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseWF2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InspectedByUserId",
                schema: "tbl",
                table: "VendorReturn",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InspectedOnUtc",
                schema: "tbl",
                table: "VendorReturn",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReceivedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BulkReceiveBatchId",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_BulkReceiveBatchId",
                schema: "tbl",
                table: "GoodsReceipt",
                column: "BulkReceiveBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipt_BulkReceiveBatchId",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "InspectedByUserId",
                schema: "tbl",
                table: "VendorReturn");

            migrationBuilder.DropColumn(
                name: "InspectedOnUtc",
                schema: "tbl",
                table: "VendorReturn");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "ApprovedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "LastReceivedOnUtc",
                schema: "tbl",
                table: "PurchaseOrder");

            migrationBuilder.DropColumn(
                name: "BulkReceiveBatchId",
                schema: "tbl",
                table: "GoodsReceipt");
        }
    }
}
