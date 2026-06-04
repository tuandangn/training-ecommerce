using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260605100000_AddBankTransferVerificationLog")]
    public partial class AddBankTransferVerificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankTransferVerificationLogs",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BankId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProviderConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransferVerificationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferVerificationLogs_PaymentIntentId",
                schema: "tbl",
                table: "BankTransferVerificationLogs",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferVerificationLogs_ProviderTransactionId",
                schema: "tbl",
                table: "BankTransferVerificationLogs",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferVerificationLogs_ReferenceCode",
                schema: "tbl",
                table: "BankTransferVerificationLogs",
                column: "ReferenceCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankTransferVerificationLogs",
                schema: "tbl");
        }
    }
}
