using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCustomerIsRetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRetailWalkInCustomer",
                schema: "tbl",
                table: "Order",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetailWalkInCustomer",
                schema: "tbl",
                table: "DeliveryNote",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRetailWalkInCustomer",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "IsRetailWalkInCustomer",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
