using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFields0904 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                schema: "tbl",
                table: "Order",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Order",
                newName: "ExpectedShippingDateUtc");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedShippingAddress",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "NormalizedShippingAddress",
                schema: "tbl",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "ExpectedShippingDateUtc",
                schema: "tbl",
                table: "Order",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                schema: "tbl",
                table: "Order",
                newName: "WarehouseId");
        }
    }
}
