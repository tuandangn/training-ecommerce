using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Update0604 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerPortalNotification",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPortalNotification", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalNotification_CustomerId_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerPortalNotification",
                columns: new[] { "CustomerId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalNotification_RelatedEntityId",
                schema: "tbl",
                table: "CustomerPortalNotification",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalNotification_Status_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerPortalNotification",
                columns: new[] { "Status", "CreatedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPortalNotification",
                schema: "tbl");
        }
    }
}
