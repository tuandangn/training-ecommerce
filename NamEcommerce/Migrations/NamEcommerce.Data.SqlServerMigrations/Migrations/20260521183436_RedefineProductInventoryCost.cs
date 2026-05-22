using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RedefineProductInventoryCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerReturnRequestItemPicture",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReturnRequestItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnRequestItemPicture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnRequestItemPicture_CustomerReturnRequestItem_CustomerReturnRequestItemId",
                        column: x => x.CustomerReturnRequestItemId,
                        principalSchema: "tbl",
                        principalTable: "CustomerReturnRequestItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboundLedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboundReferenceType = table.Column<int>(type: "int", nullable: false),
                    OutboundReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboundReferenceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InboundLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CostingStatus = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    CostingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostAllocation_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostAllocation_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "tbl",
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostingPolicy",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostingPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostLayer",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReferenceType = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReferenceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CostingStatus = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostLayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostLayer_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostLayer_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "tbl",
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostLedgerEntry",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    QuantityBalanceAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValueBalanceAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AverageCostAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostingStatus = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostLedgerEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostLedgerEntry_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostLedgerEntry_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "tbl",
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostRebuildRun",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    FromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostRebuildRun", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnRequestItemPicture_CustomerReturnRequestItemId",
                schema: "tbl",
                table: "CustomerReturnRequestItemPicture",
                column: "CustomerReturnRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnRequestItemPicture_PictureId",
                schema: "tbl",
                table: "CustomerReturnRequestItemPicture",
                column: "PictureId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_CostingRunId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "CostingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_CostingStatus",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "CostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_InboundLayerId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "InboundLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_OutboundLedgerEntryId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "OutboundLedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_OutboundReferenceType_OutboundReferenceId_OutboundReferenceItemId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                columns: new[] { "OutboundReferenceType", "OutboundReferenceId", "OutboundReferenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_ProductId_CreatedAtUtc",
                schema: "tbl",
                table: "InventoryCostAllocation",
                columns: new[] { "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_WarehouseId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostingPolicy_IsActive_EffectiveFromUtc",
                schema: "tbl",
                table: "InventoryCostingPolicy",
                columns: new[] { "IsActive", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_CostingRunId",
                schema: "tbl",
                table: "InventoryCostLayer",
                column: "CostingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_CostingStatus",
                schema: "tbl",
                table: "InventoryCostLayer",
                column: "CostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_ProductId_OpenedAtUtc",
                schema: "tbl",
                table: "InventoryCostLayer",
                columns: new[] { "ProductId", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_SourceReferenceType_SourceReferenceId_SourceReferenceItemId",
                schema: "tbl",
                table: "InventoryCostLayer",
                columns: new[] { "SourceReferenceType", "SourceReferenceId", "SourceReferenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_WarehouseId",
                schema: "tbl",
                table: "InventoryCostLayer",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_CostingRunId",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                column: "CostingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_CostingStatus",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                column: "CostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_ProductId_OccurredAtUtc_SequenceNumber",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                columns: new[] { "ProductId", "OccurredAtUtc", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_ReferenceType_ReferenceId_ReferenceItemId",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                columns: new[] { "ReferenceType", "ReferenceId", "ReferenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_WarehouseId",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostRebuildRun_ProductId_StartedAtUtc",
                schema: "tbl",
                table: "InventoryCostRebuildRun",
                columns: new[] { "ProductId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostRebuildRun_Status",
                schema: "tbl",
                table: "InventoryCostRebuildRun",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerReturnRequestItemPicture",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostingPolicy",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostLayer",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostLedgerEntry",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostRebuildRun",
                schema: "tbl");
        }
    }
}
