using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                schema: "tbl",
                table: "Order",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                schema: "tbl",
                table: "DeliveryRunItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                schema: "tbl",
                table: "DeliveryRunItem");

            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
