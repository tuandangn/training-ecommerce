using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_User_UserId",
                schema: "tbl",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "tbl",
                table: "Order",
                newName: "CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_Order_UserId",
                schema: "tbl",
                table: "Order",
                newName: "IX_Order_CustomerId");

            migrationBuilder.CreateTable(
                name: "Customer",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Customer_CustomerId",
                schema: "tbl",
                table: "Order",
                column: "CustomerId",
                principalSchema: "tbl",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Customer_CustomerId",
                schema: "tbl",
                table: "Order");

            migrationBuilder.DropTable(
                name: "Customer",
                schema: "tbl");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                schema: "tbl",
                table: "Order",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Order_CustomerId",
                schema: "tbl",
                table: "Order",
                newName: "IX_Order_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_User_UserId",
                schema: "tbl",
                table: "Order",
                column: "UserId",
                principalSchema: "tbl",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
