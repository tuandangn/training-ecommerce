using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260605090000_AddBankTransferPaymentIntentExpiry")]
    public partial class AddBankTransferPaymentIntentExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [tbl].[BankTransferPaymentIntent] SET [ExpiresAtUtc] = DATEADD(minute, 15, [CreatedOnUtc]) WHERE [ExpiresAtUtc] IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_Status_ExpiresAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                columns: new[] { "Status", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankTransferPaymentIntent_Status_ExpiresAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent");

            migrationBuilder.DropColumn(
                name: "ExpiredAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent");
        }
    }
}
