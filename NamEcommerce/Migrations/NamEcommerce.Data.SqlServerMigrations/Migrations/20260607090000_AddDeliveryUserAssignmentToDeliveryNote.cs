using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260607090000_AddDeliveryUserAssignmentToDeliveryNote")]
    public partial class AddDeliveryUserAssignmentToDeliveryNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDeliveryFullName",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDeliveryOnUtc",
                schema: "tbl",
                table: "DeliveryNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDeliveryUserId",
                schema: "tbl",
                table: "DeliveryNote",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDeliveryUsername",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNote_AssignedDeliveryUserId",
                schema: "tbl",
                table: "DeliveryNote",
                column: "AssignedDeliveryUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryNote_AssignedDeliveryUserId",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "AssignedDeliveryFullName",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "AssignedDeliveryOnUtc",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "AssignedDeliveryUserId",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "AssignedDeliveryUsername",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
