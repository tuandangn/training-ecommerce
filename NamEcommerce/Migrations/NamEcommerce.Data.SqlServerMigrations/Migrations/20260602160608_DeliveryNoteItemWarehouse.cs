using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryNoteItemWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE item
                SET item.WarehouseId = note.WarehouseId
                FROM [tbl].[DeliveryNoteItem] AS item
                INNER JOIN [tbl].[DeliveryNote] AS note ON note.Id = item.DeliveryNoteId
                WHERE item.WarehouseId IS NULL
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNoteItem",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteItem_WarehouseId",
                schema: "tbl",
                table: "DeliveryNoteItem",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryNoteItem_WarehouseId",
                schema: "tbl",
                table: "DeliveryNoteItem");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNoteItem");
        }
    }
}
