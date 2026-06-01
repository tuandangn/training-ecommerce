using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemChangeAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LastKnownLatitude",
                schema: "tbl",
                table: "CustomerPortalAccount",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastKnownLocationAccuracyMeters",
                schema: "tbl",
                table: "CustomerPortalAccount",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastKnownLocationCapturedOnUtc",
                schema: "tbl",
                table: "CustomerPortalAccount",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastKnownLocationSource",
                schema: "tbl",
                table: "CustomerPortalAccount",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastKnownLongitude",
                schema: "tbl",
                table: "CustomerPortalAccount",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerPortalSettings",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtpEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPortalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemChangeAudit",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemChangeAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemChangeAudit_Order_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "tbl",
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItemChangeAudit_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_OrderId",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_OrderId_CreatedOnUtc",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                columns: new[] { "OrderId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_OrderItemId",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_ProductId",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPortalSettings",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "OrderItemChangeAudit",
                schema: "tbl");

            migrationBuilder.DropColumn(
                name: "LastKnownLatitude",
                schema: "tbl",
                table: "CustomerPortalAccount");

            migrationBuilder.DropColumn(
                name: "LastKnownLocationAccuracyMeters",
                schema: "tbl",
                table: "CustomerPortalAccount");

            migrationBuilder.DropColumn(
                name: "LastKnownLocationCapturedOnUtc",
                schema: "tbl",
                table: "CustomerPortalAccount");

            migrationBuilder.DropColumn(
                name: "LastKnownLocationSource",
                schema: "tbl",
                table: "CustomerPortalAccount");

            migrationBuilder.DropColumn(
                name: "LastKnownLongitude",
                schema: "tbl",
                table: "CustomerPortalAccount");
        }
    }
}
