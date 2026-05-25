using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProductVendorMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductVendor",
                schema: "tbl",
                table: "ProductVendor");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductVendor",
                schema: "tbl",
                table: "ProductVendor",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendor_ProductId",
                schema: "tbl",
                table: "ProductVendor",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductVendor",
                schema: "tbl",
                table: "ProductVendor");

            migrationBuilder.DropIndex(
                name: "IX_ProductVendor_ProductId",
                schema: "tbl",
                table: "ProductVendor");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductVendor",
                schema: "tbl",
                table: "ProductVendor",
                columns: new[] { "ProductId", "VendorId" });
        }
    }
}
