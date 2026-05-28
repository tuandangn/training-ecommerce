using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompensateInNextDelivery",
                schema: "tbl",
                table: "CustomerReturnRequest",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompensateInNextDelivery",
                schema: "tbl",
                table: "CustomerReturn",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensateInNextDelivery",
                schema: "tbl",
                table: "CustomerReturnRequest");

            migrationBuilder.DropColumn(
                name: "CompensateInNextDelivery",
                schema: "tbl",
                table: "CustomerReturn");
        }
    }
}
