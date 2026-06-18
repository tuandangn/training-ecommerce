using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryWarehousePicker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "WarehouseName",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.CreateTable(
                name: "DeliveryRunWarehousePick",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedByFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConfirmedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRunWarehousePick", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRunWarehousePick_DeliveryRun_DeliveryRunId",
                        column: x => x.DeliveryRunId,
                        principalSchema: "tbl",
                        principalTable: "DeliveryRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRunWarehousePick_DeliveryRunId_WarehouseId",
                schema: "tbl",
                table: "DeliveryRunWarehousePick",
                columns: new[] { "DeliveryRunId", "WarehouseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryRunWarehousePick",
                schema: "tbl");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "tbl",
                table: "DeliveryNote",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseName",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
