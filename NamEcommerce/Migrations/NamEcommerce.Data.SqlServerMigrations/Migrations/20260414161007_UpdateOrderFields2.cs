using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                schema: "tbl",
                table: "OrderItem",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUsername",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                schema: "tbl",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "CreatedByUsername",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                schema: "tbl",
                table: "Order");
        }
    }
}
