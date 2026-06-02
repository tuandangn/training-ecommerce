using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseOrderItemChangeAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseOrderItemChangeAudit",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItemChangeAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemChangeAudit_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemChangeAudit_PurchaseOrder_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_ProductId",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_PurchaseOrderId",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_PurchaseOrderId_CreatedOnUtc",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                columns: new[] { "PurchaseOrderId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_PurchaseOrderItemId",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                column: "PurchaseOrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderItemChangeAudit",
                schema: "tbl");
        }
    }
}
