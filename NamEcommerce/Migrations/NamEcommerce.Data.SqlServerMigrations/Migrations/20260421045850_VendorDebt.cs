using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class VendorDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorDebt",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NormalizedVendorName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    VendorPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NormalizedVendorPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NormalizedVendorAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorDebt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorPayment",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VendorDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPayment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_Code",
                schema: "tbl",
                table: "VendorDebt",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_PurchaseOrderId",
                schema: "tbl",
                table: "VendorDebt",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_VendorId",
                schema: "tbl",
                table: "VendorDebt",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_Code",
                schema: "tbl",
                table: "VendorPayment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_PurchaseOrderId",
                schema: "tbl",
                table: "VendorPayment",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_VendorDebtId",
                schema: "tbl",
                table: "VendorPayment",
                column: "VendorDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_VendorId",
                schema: "tbl",
                table: "VendorPayment",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorDebt",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorPayment",
                schema: "tbl");
        }
    }
}
