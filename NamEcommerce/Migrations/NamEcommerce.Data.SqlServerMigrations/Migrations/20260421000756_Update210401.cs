using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update210401 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                schema: "tbl",
                table: "CustomerDebt",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                schema: "tbl",
                table: "CustomerDebt",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCustomerAddress",
                schema: "tbl",
                table: "CustomerDebt",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCustomerName",
                schema: "tbl",
                table: "CustomerDebt",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCustomerPhone",
                schema: "tbl",
                table: "CustomerDebt",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                schema: "tbl",
                table: "CustomerDebt");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                schema: "tbl",
                table: "CustomerDebt");

            migrationBuilder.DropColumn(
                name: "NormalizedCustomerAddress",
                schema: "tbl",
                table: "CustomerDebt");

            migrationBuilder.DropColumn(
                name: "NormalizedCustomerName",
                schema: "tbl",
                table: "CustomerDebt");

            migrationBuilder.DropColumn(
                name: "NormalizedCustomerPhone",
                schema: "tbl",
                table: "CustomerDebt");
        }
    }
}
