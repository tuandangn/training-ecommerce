using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ChangeExpenseReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Expenses",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SourceOrderId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SourceOrderId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.RenameTable(
                name: "Expenses",
                schema: "tbl",
                newName: "Expense",
                newSchema: "tbl");

            migrationBuilder.RenameColumn(
                name: "SourceVendorReturnId",
                schema: "tbl",
                table: "Expense",
                newName: "ReferenceId");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                schema: "tbl",
                table: "Expense",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferenceType",
                schema: "tbl",
                table: "Expense",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expense",
                schema: "tbl",
                table: "Expense",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Expense",
                schema: "tbl",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                schema: "tbl",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                schema: "tbl",
                table: "Expense");

            migrationBuilder.RenameTable(
                name: "Expense",
                schema: "tbl",
                newName: "Expenses",
                newSchema: "tbl");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                schema: "tbl",
                table: "Expenses",
                newName: "SourceVendorReturnId");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceOrderId",
                schema: "tbl",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expenses",
                schema: "tbl",
                table: "Expenses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceCustomerReturnId",
                unique: true,
                filter: "[SourceCustomerReturnId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceOrderId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceVendorReturnId",
                unique: true,
                filter: "[SourceVendorReturnId] IS NOT NULL");
        }
    }
}
