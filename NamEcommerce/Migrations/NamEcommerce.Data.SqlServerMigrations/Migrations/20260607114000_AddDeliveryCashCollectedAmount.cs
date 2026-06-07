using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260607114000_AddDeliveryCashCollectedAmount")]
    public partial class AddDeliveryCashCollectedAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryCashCollectedAmount",
                schema: "tbl",
                table: "DeliveryNote",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCashCollectedAmount",
                schema: "tbl",
                table: "DeliveryNote");
        }
    }
}
