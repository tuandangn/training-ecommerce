using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update070501 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                schema: "tbl",
                table: "GoodsReceipt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                schema: "tbl",
                table: "DeliveryNote",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "tbl",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
