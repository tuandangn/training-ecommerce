using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePictureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNew",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.DropColumn(
                name: "SeoName",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "tbl",
                table: "Picture",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                schema: "tbl",
                table: "Picture",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SeoName",
                schema: "tbl",
                table: "Picture",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);
        }
    }
}
