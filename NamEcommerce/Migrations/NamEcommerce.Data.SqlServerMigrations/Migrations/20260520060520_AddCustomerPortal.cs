using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerDeliveryFeedback",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDeliveryFeedback", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrderRequest",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpectedShippingDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrderRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOtpChallenge",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestedUserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentToMasked = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOtpChallenge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPaymentIntent",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderIntentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReconciledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReconciledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPaymentIntent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPortalAccount",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PasswordSalt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PasswordSetOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPortalAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPortalSession",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionTokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPortalSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnRequest",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedCustomerReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSecurityEvent",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSecurityEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryNoteAccessToken",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastViewedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNoteAccessToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrderRequestItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerOrderRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrderRequestItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerOrderRequestItem_CustomerOrderRequest_CustomerOrderRequestId",
                        column: x => x.CustomerOrderRequestId,
                        principalSchema: "tbl",
                        principalTable: "CustomerOrderRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnRequestItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReturnRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnRequestItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnRequestItem_CustomerReturnRequest_CustomerReturnRequestId",
                        column: x => x.CustomerReturnRequestId,
                        principalSchema: "tbl",
                        principalTable: "CustomerReturnRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDeliveryFeedback_CustomerId_DeliveryNoteId",
                schema: "tbl",
                table: "CustomerDeliveryFeedback",
                columns: new[] { "CustomerId", "DeliveryNoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderRequest_Code",
                schema: "tbl",
                table: "CustomerOrderRequest",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderRequest_CustomerId_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerOrderRequest",
                columns: new[] { "CustomerId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderRequest_Status",
                schema: "tbl",
                table: "CustomerOrderRequest",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderRequestItem_CustomerOrderRequestId",
                schema: "tbl",
                table: "CustomerOrderRequestItem",
                column: "CustomerOrderRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOtpChallenge_CustomerId_DeliveryNoteId_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerOtpChallenge",
                columns: new[] { "CustomerId", "DeliveryNoteId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOtpChallenge_RequestedIp_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerOtpChallenge",
                columns: new[] { "RequestedIp", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentIntent_CustomerId_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerPaymentIntent",
                columns: new[] { "CustomerId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentIntent_ProviderIntentId",
                schema: "tbl",
                table: "CustomerPaymentIntent",
                column: "ProviderIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentIntent_Status",
                schema: "tbl",
                table: "CustomerPaymentIntent",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalAccount_CustomerId",
                schema: "tbl",
                table: "CustomerPortalAccount",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalSession_CustomerId_ExpiresOnUtc",
                schema: "tbl",
                table: "CustomerPortalSession",
                columns: new[] { "CustomerId", "ExpiresOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPortalSession_SessionTokenHash",
                schema: "tbl",
                table: "CustomerPortalSession",
                column: "SessionTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnRequest_CustomerId_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerReturnRequest",
                columns: new[] { "CustomerId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnRequest_DeliveryNoteId_Status",
                schema: "tbl",
                table: "CustomerReturnRequest",
                columns: new[] { "DeliveryNoteId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnRequestItem_CustomerReturnRequestId",
                schema: "tbl",
                table: "CustomerReturnRequestItem",
                column: "CustomerReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSecurityEvent_CustomerId_EventType_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerSecurityEvent",
                columns: new[] { "CustomerId", "EventType", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSecurityEvent_IpAddress_EventType_CreatedOnUtc",
                schema: "tbl",
                table: "CustomerSecurityEvent",
                columns: new[] { "IpAddress", "EventType", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteAccessToken_DeliveryNoteId",
                schema: "tbl",
                table: "DeliveryNoteAccessToken",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteAccessToken_TokenHash",
                schema: "tbl",
                table: "DeliveryNoteAccessToken",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDeliveryFeedback",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerOrderRequestItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerOtpChallenge",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPaymentIntent",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPortalAccount",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPortalSession",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturnRequestItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerSecurityEvent",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "DeliveryNoteAccessToken",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerOrderRequest",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturnRequest",
                schema: "tbl");
        }
    }
}
