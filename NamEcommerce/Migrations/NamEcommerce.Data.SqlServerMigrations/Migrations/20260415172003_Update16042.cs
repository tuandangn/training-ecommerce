using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update16042 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Expenses",
                newName: "Expenses",
                newSchema: "tbl");

            migrationBuilder.RenameTable(
                name: "DeliveryNoteItem",
                newName: "DeliveryNoteItem",
                newSchema: "tbl");

            migrationBuilder.RenameTable(
                name: "DeliveryNote",
                newName: "DeliveryNote",
                newSchema: "tbl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Expenses",
                schema: "tbl",
                newName: "Expenses");

            migrationBuilder.RenameTable(
                name: "DeliveryNoteItem",
                schema: "tbl",
                newName: "DeliveryNoteItem");

            migrationBuilder.RenameTable(
                name: "DeliveryNote",
                schema: "tbl",
                newName: "DeliveryNote");
        }
    }
}
