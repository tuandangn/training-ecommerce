using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                schema: "tbl",
                table: "VendorPayment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpeningBalance",
                schema: "tbl",
                table: "VendorDebt",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "tbl",
                table: "PurchaseOrderItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "tbl",
                table: "PurchaseOrderItem",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "tbl",
                table: "GoodsReceiptItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "tbl",
                table: "GoodsReceiptItem",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VendorInvoiceDate",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorInvoiceNumber",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                schema: "tbl",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                schema: "tbl",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "tbl",
                table: "Expenses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "tbl",
                table: "Expenses",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                schema: "tbl",
                table: "DeliveryNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceSeries",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                schema: "tbl",
                table: "CustomerRefund",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                schema: "tbl",
                table: "CustomerPayment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpeningBalance",
                schema: "tbl",
                table: "CustomerDebt",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AccountingSetup",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FiscalYearStartDay = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AccountingStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningEquity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultTaxRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false, defaultValue: 0.10m),
                    CorporateTaxProvision = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FinalizedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSetup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankAccount",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FixedAsset",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CostCenter = table.Column<int>(type: "int", nullable: false),
                    AcquisitionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    ResidualValue = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorInvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisposedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAsset", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccount_Code",
                schema: "tbl",
                table: "BankAccount",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAsset_Code",
                schema: "tbl",
                table: "FixedAsset",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingSetup",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "BankAccount",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "FixedAsset",
                schema: "tbl");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                schema: "tbl",
                table: "VendorPayment");

            migrationBuilder.DropColumn(
                name: "IsOpeningBalance",
                schema: "tbl",
                table: "VendorDebt");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "tbl",
                table: "PurchaseOrderItem");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "tbl",
                table: "PurchaseOrderItem");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "tbl",
                table: "GoodsReceiptItem");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "tbl",
                table: "GoodsReceiptItem");

            migrationBuilder.DropColumn(
                name: "VendorInvoiceDate",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "VendorInvoiceNumber",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "tbl",
                table: "DeliveryNoteItem");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                schema: "tbl",
                table: "DeliveryNoteItem");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "tbl",
                table: "DeliveryNoteItem");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "tbl",
                table: "DeliveryNoteItem");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "InvoiceSeries",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                schema: "tbl",
                table: "CustomerRefund");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                schema: "tbl",
                table: "CustomerPayment");

            migrationBuilder.DropColumn(
                name: "IsOpeningBalance",
                schema: "tbl",
                table: "CustomerDebt");
        }
    }
}
