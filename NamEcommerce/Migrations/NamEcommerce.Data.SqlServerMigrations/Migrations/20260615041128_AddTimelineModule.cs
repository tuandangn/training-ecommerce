using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTimelineModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderFulfillmentSchedule",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScheduledFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InactivatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFulfillmentSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFulfillmentSchedule_Order_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "tbl",
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderFulfillmentScheduleItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderFulfillmentScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFulfillmentScheduleItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFulfillmentScheduleItem_OrderFulfillmentSchedule_OrderFulfillmentScheduleId",
                        column: x => x.OrderFulfillmentScheduleId,
                        principalSchema: "tbl",
                        principalTable: "OrderFulfillmentSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderFulfillmentScheduleItem_OrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "tbl",
                        principalTable: "OrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentSchedule_IsActive",
                schema: "tbl",
                table: "OrderFulfillmentSchedule",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentSchedule_Mode",
                schema: "tbl",
                table: "OrderFulfillmentSchedule",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentSchedule_OrderId",
                schema: "tbl",
                table: "OrderFulfillmentSchedule",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentSchedule_ScheduledFromUtc",
                schema: "tbl",
                table: "OrderFulfillmentSchedule",
                column: "ScheduledFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentScheduleItem_OrderFulfillmentScheduleId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem",
                column: "OrderFulfillmentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentScheduleItem_OrderItemId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfillmentScheduleItem_ProductId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderFulfillmentScheduleItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "OrderFulfillmentSchedule",
                schema: "tbl");
        }
    }
}
