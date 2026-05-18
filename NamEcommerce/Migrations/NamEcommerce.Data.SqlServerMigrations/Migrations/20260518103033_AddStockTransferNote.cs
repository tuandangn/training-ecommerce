using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTransferNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromWarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToWarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StockTransferNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferNoteItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferNoteItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferNoteItem_StockTransferNote_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "tbl",
                        principalTable: "StockTransferNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNote_Code",
                schema: "tbl",
                table: "StockTransferNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNote_CreatedOnUtc",
                schema: "tbl",
                table: "StockTransferNote",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNote_FromWarehouseId_Status",
                schema: "tbl",
                table: "StockTransferNote",
                columns: new[] { "FromWarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNoteItem_NoteId_ProductId",
                schema: "tbl",
                table: "StockTransferNoteItem",
                columns: new[] { "NoteId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransferNoteItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockTransferNote",
                schema: "tbl");
        }
    }
}
