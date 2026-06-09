using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemNotification",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequiredPermission = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotificationRead",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotificationRead", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotification_RelatedEntityId",
                schema: "tbl",
                table: "SystemNotification",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotification_RequiredPermission_CreatedOnUtc",
                schema: "tbl",
                table: "SystemNotification",
                columns: new[] { "RequiredPermission", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotification_Type_CreatedOnUtc",
                schema: "tbl",
                table: "SystemNotification",
                columns: new[] { "Type", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotificationRead_NotificationId_UserId",
                schema: "tbl",
                table: "SystemNotificationRead",
                columns: new[] { "NotificationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotificationRead_UserId_ReadOnUtc",
                schema: "tbl",
                table: "SystemNotificationRead",
                columns: new[] { "UserId", "ReadOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemNotification",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "SystemNotificationRead",
                schema: "tbl");
        }
    }
}
