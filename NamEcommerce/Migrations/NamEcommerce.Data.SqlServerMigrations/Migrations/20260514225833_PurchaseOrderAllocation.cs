using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseOrderAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "tbl",
                table: "VendorDebt");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "tbl",
                table: "CustomerDebt");

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItemAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItemAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemAllocation_OrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "tbl",
                        principalTable: "OrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemAllocation_PurchaseOrderItem_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemAllocation_OrderItemId",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemAllocation_PurchaseOrderItemId",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                column: "PurchaseOrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderItemAllocation",
                schema: "tbl");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "tbl",
                table: "VendorDebt",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "tbl",
                table: "CustomerDebt",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
