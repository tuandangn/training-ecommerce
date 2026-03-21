using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedAddress",
                schema: "tbl",
                table: "User",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedFullName",
                schema: "tbl",
                table: "User",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "tbl",
                table: "UnitMeasurement",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "tbl",
                table: "Role",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "tbl",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedShortDesc",
                schema: "tbl",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "tbl",
                table: "Permission",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "tbl",
                table: "Category",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedAddress",
                schema: "tbl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "NormalizedFullName",
                schema: "tbl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "tbl",
                table: "UnitMeasurement");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "tbl",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "tbl",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "NormalizedShortDesc",
                schema: "tbl",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "tbl",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "tbl",
                table: "Category");
        }
    }
}
