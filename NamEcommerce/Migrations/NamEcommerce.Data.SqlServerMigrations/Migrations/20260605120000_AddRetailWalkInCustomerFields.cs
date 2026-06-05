using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260605120000_AddRetailWalkInCustomerFields")]
    public partial class AddRetailWalkInCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerKind",
                schema: "tbl",
                table: "Customer",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                schema: "tbl",
                table: "Customer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CustomerKind_IsSystem",
                schema: "tbl",
                table: "Customer",
                columns: new[] { "CustomerKind", "IsSystem" },
                unique: true,
                filter: "[CustomerKind] = 20 AND [IsSystem] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customer_CustomerKind_IsSystem",
                schema: "tbl",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "CustomerKind",
                schema: "tbl",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                schema: "tbl",
                table: "Customer");
        }
    }
}
