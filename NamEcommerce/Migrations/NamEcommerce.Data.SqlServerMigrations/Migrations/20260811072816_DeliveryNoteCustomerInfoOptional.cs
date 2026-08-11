using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryNoteCustomerInfoOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderFulfillmentScheduleItem_OrderItem_OrderItemId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRetailWalkInCustomer",
                schema: "tbl",
                table: "DeliveryNote",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerAddress",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFulfillmentScheduleItem_OrderItem_OrderItemId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem",
                column: "OrderItemId",
                principalSchema: "tbl",
                principalTable: "OrderItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderFulfillmentScheduleItem_OrderItem_OrderItemId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRetailWalkInCustomer",
                schema: "tbl",
                table: "DeliveryNote",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerAddress",
                schema: "tbl",
                table: "DeliveryNote",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderFulfillmentScheduleItem_OrderItem_OrderItemId",
                schema: "tbl",
                table: "OrderFulfillmentScheduleItem",
                column: "OrderItemId",
                principalSchema: "tbl",
                principalTable: "OrderItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
