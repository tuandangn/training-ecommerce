using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AdminCanChangeAmountToCollect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AmountToCollectOverriddenAt",
                schema: "tbl",
                table: "DeliveryNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmountToCollectOverrideNote",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountToCollectOverriddenAt",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "AmountToCollectOverrideNote",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
