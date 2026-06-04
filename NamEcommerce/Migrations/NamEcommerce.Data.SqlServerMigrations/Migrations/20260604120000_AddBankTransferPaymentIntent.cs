using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260604120000_AddBankTransferPaymentIntent")]
    public partial class AddBankTransferPaymentIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankTransferPaymentIntent",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QrImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerificationSource = table.Column<int>(type: "int", nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransferPaymentIntent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_CustomerDebtId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "CustomerDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_CustomerId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_CustomerPaymentId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "CustomerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_DeliveryNoteId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_OrderId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_ProviderTransactionId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_ReferenceCode",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_Status_CreatedOnUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                columns: new[] { "Status", "CreatedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankTransferPaymentIntent",
                schema: "tbl");
        }
    }
}
