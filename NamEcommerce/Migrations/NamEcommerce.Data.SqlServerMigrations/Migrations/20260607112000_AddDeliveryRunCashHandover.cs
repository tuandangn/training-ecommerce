using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260607112000_AddDeliveryRunCashHandover")]
    public partial class AddDeliveryRunCashHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashHandoverAmount",
                schema: "tbl",
                table: "DeliveryRun",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashHandoverConfirmedByFullName",
                schema: "tbl",
                table: "DeliveryRun",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashHandoverConfirmedByUserId",
                schema: "tbl",
                table: "DeliveryRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashHandoverConfirmedByUsername",
                schema: "tbl",
                table: "DeliveryRun",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CashHandoverConfirmedOnUtc",
                schema: "tbl",
                table: "DeliveryRun",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashHandoverNote",
                schema: "tbl",
                table: "DeliveryRun",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashHandoverAmount",
                schema: "tbl",
                table: "DeliveryRun");

            migrationBuilder.DropColumn(
                name: "CashHandoverConfirmedByFullName",
                schema: "tbl",
                table: "DeliveryRun");

            migrationBuilder.DropColumn(
                name: "CashHandoverConfirmedByUserId",
                schema: "tbl",
                table: "DeliveryRun");

            migrationBuilder.DropColumn(
                name: "CashHandoverConfirmedByUsername",
                schema: "tbl",
                table: "DeliveryRun");

            migrationBuilder.DropColumn(
                name: "CashHandoverConfirmedOnUtc",
                schema: "tbl",
                table: "DeliveryRun");

            migrationBuilder.DropColumn(
                name: "CashHandoverNote",
                schema: "tbl",
                table: "DeliveryRun");
        }
    }
}
