using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RefindOrderWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockOrderReason",
                schema: "tbl",
                table: "Order");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOnUtc",
                schema: "tbl",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceOrderId",
                schema: "tbl",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceOrderId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expenses_SourceOrderId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CompletedOnUtc",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SourceOrderId",
                schema: "tbl",
                table: "Expenses");

            migrationBuilder.AddColumn<string>(
                name: "LockOrderReason",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
