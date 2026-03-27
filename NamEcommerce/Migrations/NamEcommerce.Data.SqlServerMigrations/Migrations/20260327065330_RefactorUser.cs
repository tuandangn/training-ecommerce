using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRole_User_UserId",
                schema: "tbl",
                table: "UserRole");

            migrationBuilder.DropIndex(
                name: "IX_UserRole_UserId",
                schema: "tbl",
                table: "UserRole");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.DropColumn(
                name: "UpdatedOnUtc",
                schema: "tbl",
                table: "Picture");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Category");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordSalt",
                schema: "tbl",
                table: "User",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordSalt",
                schema: "tbl",
                table: "User",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "User",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Product",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Picture",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOnUtc",
                schema: "tbl",
                table: "Picture",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                schema: "tbl",
                table: "Category",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                schema: "tbl",
                table: "UserRole",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRole_User_UserId",
                schema: "tbl",
                table: "UserRole",
                column: "UserId",
                principalSchema: "tbl",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
