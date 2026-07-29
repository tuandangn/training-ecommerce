using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryNotePaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AppliedOrderDiscount",
                schema: "tbl",
                table: "DeliveryNote",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedOrderPrepaid",
                schema: "tbl",
                table: "DeliveryNote",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedOrderDiscount",
                schema: "tbl",
                table: "DeliveryNote");

            migrationBuilder.DropColumn(
                name: "AppliedOrderPrepaid",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
