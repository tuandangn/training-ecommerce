using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DebtCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                schema: "tbl",
                table: "UnitMeasurement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CustomerCreditNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceDeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCreditNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorCreditNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceGoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourcePurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorCreditNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCreditNoteAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCreditNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerDebtCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReverseReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCreditNoteAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNoteAllocation_CustomerCreditNote_CustomerCreditNoteId",
                        column: x => x.CustomerCreditNoteId,
                        principalSchema: "tbl",
                        principalTable: "CustomerCreditNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorCreditNoteAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorCreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorCreditNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorDebtCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReverseReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorCreditNoteAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorCreditNoteAllocation_VendorCreditNote_VendorCreditNoteId",
                        column: x => x.VendorCreditNoteId,
                        principalSchema: "tbl",
                        principalTable: "VendorCreditNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_Code",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_CustomerId",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_SourceDeliveryNoteId",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "SourceDeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_SourceReturnId",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNoteAllocation_CustomerCreditNoteId",
                schema: "tbl",
                table: "CustomerCreditNoteAllocation",
                column: "CustomerCreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNoteAllocation_CustomerDebtId",
                schema: "tbl",
                table: "CustomerCreditNoteAllocation",
                column: "CustomerDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNoteAllocation_SourceReturnId",
                schema: "tbl",
                table: "CustomerCreditNoteAllocation",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_Code",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_SourceGoodsReceiptId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "SourceGoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_SourcePurchaseOrderId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "SourcePurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_SourceReturnId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_VendorId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNoteAllocation_SourceReturnId",
                schema: "tbl",
                table: "VendorCreditNoteAllocation",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNoteAllocation_VendorCreditNoteId",
                schema: "tbl",
                table: "VendorCreditNoteAllocation",
                column: "VendorCreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNoteAllocation_VendorDebtId",
                schema: "tbl",
                table: "VendorCreditNoteAllocation",
                column: "VendorDebtId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerCreditNoteAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorCreditNoteAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerCreditNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorCreditNote",
                schema: "tbl");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                schema: "tbl",
                table: "UnitMeasurement");
        }
    }
}
