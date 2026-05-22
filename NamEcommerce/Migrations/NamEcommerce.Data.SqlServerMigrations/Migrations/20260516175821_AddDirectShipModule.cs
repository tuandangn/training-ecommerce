using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectShipModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectShipAddress",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectShipContactName",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectShipContactPhone",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectShipPriority",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirectShip",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                schema: "tbl",
                table: "DeliveryNote",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedNote",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryConfirmationStatus",
                schema: "tbl",
                table: "DeliveryNote",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirectShip",
                schema: "tbl",
                table: "DeliveryNote",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceGoodsReceiptId",
                schema: "tbl",
                table: "DeliveryNote",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DirectShipAddressChangeLog",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NewAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OldContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OldContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectShipAddressChangeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectShipAddressChangeLog_PurchaseOrderItemAllocation_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrderItemAllocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectShipAddressChangeLog_AllocationId",
                schema: "tbl",
                table: "DirectShipAddressChangeLog",
                column: "AllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectShipAddressChangeLog",
                schema: "tbl");

            migrationBuilder.DropColumn(
                name: "DirectShipAddress",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation");

            migrationBuilder.DropColumn(
                name: "DirectShipContactName",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation");

            migrationBuilder.DropColumn(
                name: "DirectShipContactPhone",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation");

            migrationBuilder.DropColumn(
                name: "DirectShipPriority",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation");

            migrationBuilder.DropColumn(
                name: "IsDirectShip",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "ConfirmedNote",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "DeliveryConfirmationStatus",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "IsDirectShip",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "SourceGoodsReceiptId",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
