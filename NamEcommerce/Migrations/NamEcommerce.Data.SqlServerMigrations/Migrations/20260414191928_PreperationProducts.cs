using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PreperationProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredOnUtc",
                schema: "tbl",
                table: "OrderItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryProofPictureId",
                schema: "tbl",
                table: "OrderItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivered",
                schema: "tbl",
                table: "OrderItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredOnUtc",
                schema: "tbl",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "DeliveryProofPictureId",
                schema: "tbl",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "IsDelivered",
                schema: "tbl",
                table: "OrderItem");
        }
    }
}
