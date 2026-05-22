using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update080501 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostAtDispatch",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerReturn",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedGoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturn", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorReturn",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedDeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorReturn", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeliveryNoteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnItem_CustomerReturn_CustomerReturnId",
                        column: x => x.CustomerReturnId,
                        principalSchema: "tbl",
                        principalTable: "CustomerReturn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentNoteItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PhysicalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentNoteItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentNoteItem_StockAdjustmentNote_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "tbl",
                        principalTable: "StockAdjustmentNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorReturnItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GoodsReceiptItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorReturnItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorReturnItem_VendorReturn_VendorReturnId",
                        column: x => x.VendorReturnId,
                        principalSchema: "tbl",
                        principalTable: "VendorReturn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_Code",
                schema: "tbl",
                table: "CustomerReturn",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_CustomerId_Status",
                schema: "tbl",
                table: "CustomerReturn",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_OrderId_Status",
                schema: "tbl",
                table: "CustomerReturn",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnItem_CustomerReturnId",
                schema: "tbl",
                table: "CustomerReturnItem",
                column: "CustomerReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNote_Code",
                schema: "tbl",
                table: "StockAdjustmentNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNote_CreatedOnUtc",
                schema: "tbl",
                table: "StockAdjustmentNote",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNote_WarehouseId_Status",
                schema: "tbl",
                table: "StockAdjustmentNote",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNoteItem_NoteId_ProductId",
                schema: "tbl",
                table: "StockAdjustmentNoteItem",
                columns: new[] { "NoteId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_Code",
                schema: "tbl",
                table: "VendorReturn",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_GoodsReceiptId_Status",
                schema: "tbl",
                table: "VendorReturn",
                columns: new[] { "GoodsReceiptId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_PurchaseOrderId_Status",
                schema: "tbl",
                table: "VendorReturn",
                columns: new[] { "PurchaseOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_VendorId_Status",
                schema: "tbl",
                table: "VendorReturn",
                columns: new[] { "VendorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturnItem_VendorReturnId",
                schema: "tbl",
                table: "VendorReturnItem",
                column: "VendorReturnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerReturnItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockAdjustmentNoteItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorReturnItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturn",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockAdjustmentNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorReturn",
                schema: "tbl");

            migrationBuilder.DropColumn(
                name: "CostAtDispatch",
                schema: "tbl",
                table: "DeliveryNoteItem");
        }
    }
}
