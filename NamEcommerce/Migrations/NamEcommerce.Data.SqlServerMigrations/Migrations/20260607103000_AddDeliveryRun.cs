using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260607103000_AddDeliveryRun")]
    public partial class AddDeliveryRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryRun",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssignedDeliveryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedDeliveryUsername = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedDeliveryFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    PreparedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreparedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandedOverByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandedOverOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DriverCachedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DriverCacheDeviceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaperManifestIssued = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PaperManifestIssuedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryRunItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AmountToCollect = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRunItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRunItem_DeliveryRun_DeliveryRunId",
                        column: x => x.DeliveryRunId,
                        principalSchema: "tbl",
                        principalTable: "DeliveryRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRun_Code",
                schema: "tbl",
                table: "DeliveryRun",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRun_CreatedOnUtc",
                schema: "tbl",
                table: "DeliveryRun",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRun_AssignedDeliveryUserId_Status",
                schema: "tbl",
                table: "DeliveryRun",
                columns: new[] { "AssignedDeliveryUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRunItem_DeliveryRunId",
                schema: "tbl",
                table: "DeliveryRunItem",
                column: "DeliveryRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRunItem_DeliveryNoteId",
                schema: "tbl",
                table: "DeliveryRunItem",
                column: "DeliveryNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryRunItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "DeliveryRun",
                schema: "tbl");
        }
    }
}
