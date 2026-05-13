using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReservationLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductReservation",
                schema: "tbl");

            migrationBuilder.CreateTable(
                name: "ProductReservationLedger",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReservationLedger", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservationLedger_ProductId",
                schema: "tbl",
                table: "ProductReservationLedger",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservationLedger_ProductId_OrderId",
                schema: "tbl",
                table: "ProductReservationLedger",
                columns: new[] { "ProductId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservationLedger_ReferenceId",
                schema: "tbl",
                table: "ProductReservationLedger",
                column: "ReferenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductReservationLedger",
                schema: "tbl");

            migrationBuilder.CreateTable(
                name: "ProductReservation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalReservedByOrder = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReservation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservation_ProductId",
                schema: "tbl",
                table: "ProductReservation",
                column: "ProductId",
                unique: true);
        }
    }
}
