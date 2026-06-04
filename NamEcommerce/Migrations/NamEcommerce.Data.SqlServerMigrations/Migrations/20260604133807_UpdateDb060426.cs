using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamEcommerce.Data.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDb060426 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tbl");

            migrationBuilder.CreateTable(
                name: "BankTransferPaymentIntent",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QrImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerificationSource = table.Column<int>(type: "int", nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransferPaymentIntent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NormalizedAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NormalizedFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCreditNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceDeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCreditNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDebt",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NormalizedCustomerName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NormalizedCustomerPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NormalizedCustomerAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDebt", x => x.Id);
                });

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
                name: "CustomerPayment",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayment", x => x.Id);
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
                    LastKnownLatitude = table.Column<double>(type: "float", nullable: true),
                    LastKnownLongitude = table.Column<double>(type: "float", nullable: true),
                    LastKnownLocationAccuracyMeters = table.Column<double>(type: "float", nullable: true),
                    LastKnownLocationCapturedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastKnownLocationSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                name: "CustomerPortalSettings",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtpEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPortalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRefund",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CustomerReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReturnCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RefundedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRefund", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturn",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdditionalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CompensateInNextDelivery = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConfirmedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeneratedGoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturn", x => x.Id);
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
                    CompensateInNextDelivery = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                name: "DeliveryNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShowPrice = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Surcharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SurchargeReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountToCollect = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDirectShip = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeliveryConfirmationStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceGoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryProofPictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryProofPictureIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryReceiverName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNote", x => x.Id);
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
                name: "ExpenseBudgets",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    IncurredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceVendorReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceCustomerReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipt",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    ReceivedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BulkReceiveBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TruckNumberSerial = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUsername = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PictureIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TruckDriverNameNormalized = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TruckDriverName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VendorAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VendorPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostingPolicy",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostingPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostRebuildRun",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    FromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostRebuildRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryStock",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxStockLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastStocktakeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReservedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Picture",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Picture", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ShortDesc = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    NormalizedShortDesc = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    UnitMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductReservationLedger",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReservationLedger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrder",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlacedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpectedDeliveryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CloseReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClosedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReceivedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedShippingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    AccumulatedTaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockAuditLog",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PerformedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementLog",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromWarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToWarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitMeasurement",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitMeasurement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedFullName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NormalizedAddress = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendor",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NormalizedAddress = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorCreditNote",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceGoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourcePurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorCreditNote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorDebt",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NormalizedVendorName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    VendorPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NormalizedVendorPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NormalizedVendorAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorDebt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorPayment",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VendorDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPayment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorReturn",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdditionalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    GeneratedDeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorReturn", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    NormalizedAddress = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpectedShippingDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderSubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderStatus = table.Column<int>(type: "int", nullable: false),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUsername = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "tbl",
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCreditNoteAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCreditNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerDebtCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReverseReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCreditNoteAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNoteAllocation_CustomerCreditNote_CustomerCreditNoteId",
                        column: x => x.CustomerCreditNoteId,
                        principalSchema: "tbl",
                        principalTable: "CustomerCreditNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "CustomerReturnItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeliveryNoteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalUnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReturnUnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnItem_CustomerReturn_CustomerReturnId",
                        column: x => x.CustomerReturnId,
                        principalSchema: "tbl",
                        principalTable: "CustomerReturn",
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

            migrationBuilder.CreateTable(
                name: "DeliveryNoteItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostAtDispatch = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNoteItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNoteItem_DeliveryNote_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalSchema: "tbl",
                        principalTable: "DeliveryNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItem_GoodsReceipt_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "tbl",
                        principalTable: "GoodsReceipt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategory",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategory_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "tbl",
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductCategory_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPicture",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPicture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPicture_Picture_PictureId",
                        column: x => x.PictureId,
                        principalSchema: "tbl",
                        principalTable: "Picture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPicture_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPriceHistory",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldCostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewCostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistory_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityOrdered = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_PurchaseOrder_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItemChangeAudit",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItemChangeAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemChangeAudit_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemChangeAudit_PurchaseOrder_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "tbl",
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "tbl",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "tbl",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentNoteItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PhysicalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentNoteItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentNoteItem_StockAdjustmentNote_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "tbl",
                        principalTable: "StockAdjustmentNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferNoteItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferNoteItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferNoteItem_StockTransferNote_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "tbl",
                        principalTable: "StockTransferNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVendor",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVendor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVendor_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVendor_Vendor_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "tbl",
                        principalTable: "Vendor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorCreditNoteAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorCreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorCreditNoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorDebtCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReverseReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorCreditNoteAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorCreditNoteAllocation_VendorCreditNote_VendorCreditNoteId",
                        column: x => x.VendorCreditNoteId,
                        principalSchema: "tbl",
                        principalTable: "VendorCreditNote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorReturnItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GoodsReceiptItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReturnUnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorReturnItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorReturnItem_VendorReturn_VendorReturnId",
                        column: x => x.VendorReturnId,
                        principalSchema: "tbl",
                        principalTable: "VendorReturn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboundLedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboundReferenceType = table.Column<int>(type: "int", nullable: false),
                    OutboundReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboundReferenceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InboundLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CostingStatus = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    CostingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostAllocation_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostAllocation_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "tbl",
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostLayer",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReferenceType = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReferenceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CostingStatus = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostLayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostLayer_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostLayer_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "tbl",
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostLedgerEntry",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    QuantityBalanceAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValueBalanceAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AverageCostAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostingStatus = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<int>(type: "int", nullable: false),
                    ValuationScope = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostLedgerEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostLedgerEntry_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCostLedgerEntry_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "tbl",
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeliveredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryProofPictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "tbl",
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemChangeAudit",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemChangeAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemChangeAudit_Order_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "tbl",
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItemChangeAudit_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "tbl",
                        principalTable: "Product",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnRequestItemPicture",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReturnRequestItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnRequestItemPicture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnRequestItemPicture_CustomerReturnRequestItem_CustomerReturnRequestItemId",
                        column: x => x.CustomerReturnRequestItemId,
                        principalSchema: "tbl",
                        principalTable: "CustomerReturnRequestItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItemAllocation",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDirectShip = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DirectShipAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DirectShipContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DirectShipContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DirectShipPriority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItemAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemAllocation_OrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "tbl",
                        principalTable: "OrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItemAllocation_PurchaseOrderItem_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectShipAddressChangeLog",
                schema: "tbl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NewAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OldContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OldContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectShipAddressChangeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectShipAddressChangeLog_PurchaseOrderItemAllocation_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "tbl",
                        principalTable: "PurchaseOrderItemAllocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_CustomerDebtId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "CustomerDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_CustomerId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_CustomerPaymentId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "CustomerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_DeliveryNoteId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_OrderId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_ProviderTransactionId",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_ReferenceCode",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPaymentIntent_Status_CreatedOnUtc",
                schema: "tbl",
                table: "BankTransferPaymentIntent",
                columns: new[] { "Status", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_Code",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_CustomerId",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_SourceDeliveryNoteId",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "SourceDeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNote_SourceReturnId",
                schema: "tbl",
                table: "CustomerCreditNote",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNoteAllocation_CustomerCreditNoteId",
                schema: "tbl",
                table: "CustomerCreditNoteAllocation",
                column: "CustomerCreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNoteAllocation_CustomerDebtId",
                schema: "tbl",
                table: "CustomerCreditNoteAllocation",
                column: "CustomerDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNoteAllocation_SourceReturnId",
                schema: "tbl",
                table: "CustomerCreditNoteAllocation",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebt_Code",
                schema: "tbl",
                table: "CustomerDebt",
                column: "Code",
                unique: true);

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
                name: "IX_CustomerPayment_Code",
                schema: "tbl",
                table: "CustomerPayment",
                column: "Code",
                unique: true);

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
                name: "IX_CustomerRefund_Code",
                schema: "tbl",
                table: "CustomerRefund",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefund_CustomerId_Status",
                schema: "tbl",
                table: "CustomerRefund",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefund_CustomerReturnId",
                schema: "tbl",
                table: "CustomerRefund",
                column: "CustomerReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_Code",
                schema: "tbl",
                table: "CustomerReturn",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_CustomerId_Status",
                schema: "tbl",
                table: "CustomerReturn",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturn_DeliveryNoteId_Status",
                schema: "tbl",
                table: "CustomerReturn",
                columns: new[] { "DeliveryNoteId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnItem_CustomerReturnId",
                schema: "tbl",
                table: "CustomerReturnItem",
                column: "CustomerReturnId");

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
                name: "IX_CustomerReturnRequestItemPicture_CustomerReturnRequestItemId",
                schema: "tbl",
                table: "CustomerReturnRequestItemPicture",
                column: "CustomerReturnRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnRequestItemPicture_PictureId",
                schema: "tbl",
                table: "CustomerReturnRequestItemPicture",
                column: "PictureId");

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
                name: "IX_DeliveryNote_Code",
                schema: "tbl",
                table: "DeliveryNote",
                column: "Code",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteItem_DeliveryNoteId",
                schema: "tbl",
                table: "DeliveryNoteItem",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteItem_WarehouseId",
                schema: "tbl",
                table: "DeliveryNoteItem",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectShipAddressChangeLog_AllocationId",
                schema: "tbl",
                table: "DirectShipAddressChangeLog",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_ExpenseType_Year_Month",
                schema: "tbl",
                table: "ExpenseBudgets",
                columns: new[] { "ExpenseType", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceCustomerReturnId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceCustomerReturnId",
                unique: true,
                filter: "[SourceCustomerReturnId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceOrderId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SourceVendorReturnId",
                schema: "tbl",
                table: "Expenses",
                column: "SourceVendorReturnId",
                unique: true,
                filter: "[SourceVendorReturnId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_BulkReceiveBatchId",
                schema: "tbl",
                table: "GoodsReceipt",
                column: "BulkReceiveBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_Code",
                schema: "tbl",
                table: "GoodsReceipt",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItem_GoodsReceiptId",
                schema: "tbl",
                table: "GoodsReceiptItem",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItem_ProductId",
                schema: "tbl",
                table: "GoodsReceiptItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_CostingRunId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "CostingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_CostingStatus",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "CostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_InboundLayerId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "InboundLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_OutboundLedgerEntryId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "OutboundLedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_OutboundReferenceType_OutboundReferenceId_OutboundReferenceItemId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                columns: new[] { "OutboundReferenceType", "OutboundReferenceId", "OutboundReferenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_ProductId_CreatedAtUtc",
                schema: "tbl",
                table: "InventoryCostAllocation",
                columns: new[] { "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocation_WarehouseId",
                schema: "tbl",
                table: "InventoryCostAllocation",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostingPolicy_IsActive_EffectiveFromUtc",
                schema: "tbl",
                table: "InventoryCostingPolicy",
                columns: new[] { "IsActive", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_CostingRunId",
                schema: "tbl",
                table: "InventoryCostLayer",
                column: "CostingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_CostingStatus",
                schema: "tbl",
                table: "InventoryCostLayer",
                column: "CostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_ProductId_OpenedAtUtc",
                schema: "tbl",
                table: "InventoryCostLayer",
                columns: new[] { "ProductId", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_SourceReferenceType_SourceReferenceId_SourceReferenceItemId",
                schema: "tbl",
                table: "InventoryCostLayer",
                columns: new[] { "SourceReferenceType", "SourceReferenceId", "SourceReferenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_WarehouseId",
                schema: "tbl",
                table: "InventoryCostLayer",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_CostingRunId",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                column: "CostingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_CostingStatus",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                column: "CostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_ProductId_OccurredAtUtc_SequenceNumber",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                columns: new[] { "ProductId", "OccurredAtUtc", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_ReferenceType_ReferenceId_ReferenceItemId",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                columns: new[] { "ReferenceType", "ReferenceId", "ReferenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLedgerEntry_WarehouseId",
                schema: "tbl",
                table: "InventoryCostLedgerEntry",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostRebuildRun_ProductId_StartedAtUtc",
                schema: "tbl",
                table: "InventoryCostRebuildRun",
                columns: new[] { "ProductId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostRebuildRun_Status",
                schema: "tbl",
                table: "InventoryCostRebuildRun",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStock_ProductId_WarehouseId_WarehouseZoneId",
                schema: "tbl",
                table: "InventoryStock",
                columns: new[] { "ProductId", "WarehouseId", "WarehouseZoneId" },
                unique: true,
                filter: "[WarehouseZoneId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Code",
                schema: "tbl",
                table: "Order",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_CustomerId",
                schema: "tbl",
                table: "Order",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                schema: "tbl",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ProductId",
                schema: "tbl",
                table: "OrderItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_OrderId",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_OrderId_CreatedOnUtc",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                columns: new[] { "OrderId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_OrderItemId",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeAudit_ProductId",
                schema: "tbl",
                table: "OrderItemChangeAudit",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_Pending",
                schema: "tbl",
                table: "OutboxMessage",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategory_CategoryId",
                schema: "tbl",
                table: "ProductCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategory_ProductId",
                schema: "tbl",
                table: "ProductCategory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPicture_PictureId",
                schema: "tbl",
                table: "ProductPicture",
                column: "PictureId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPicture_ProductId",
                schema: "tbl",
                table: "ProductPicture",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistory_ProductId",
                schema: "tbl",
                table: "ProductPriceHistory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservationLedger_ProductId",
                schema: "tbl",
                table: "ProductReservationLedger",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservationLedger_ProductId_OrderId",
                schema: "tbl",
                table: "ProductReservationLedger",
                columns: new[] { "ProductId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReservationLedger_ReferenceId",
                schema: "tbl",
                table: "ProductReservationLedger",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendor_ProductId",
                schema: "tbl",
                table: "ProductVendor",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendor_VendorId",
                schema: "tbl",
                table: "ProductVendor",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_Code",
                schema: "tbl",
                table: "PurchaseOrder",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_ProductId",
                schema: "tbl",
                table: "PurchaseOrderItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_PurchaseOrderId",
                schema: "tbl",
                table: "PurchaseOrderItem",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemAllocation_OrderItemId",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemAllocation_PurchaseOrderItemId",
                schema: "tbl",
                table: "PurchaseOrderItemAllocation",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_ProductId",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_PurchaseOrderId",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_PurchaseOrderId_CreatedOnUtc",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                columns: new[] { "PurchaseOrderId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItemChangeAudit_PurchaseOrderItemId",
                schema: "tbl",
                table: "PurchaseOrderItemChangeAudit",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                schema: "tbl",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId",
                schema: "tbl",
                table: "RolePermission",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNote_Code",
                schema: "tbl",
                table: "StockAdjustmentNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNote_CreatedOnUtc",
                schema: "tbl",
                table: "StockAdjustmentNote",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNote_WarehouseId_Status",
                schema: "tbl",
                table: "StockAdjustmentNote",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentNoteItem_NoteId_ProductId",
                schema: "tbl",
                table: "StockAdjustmentNoteItem",
                columns: new[] { "NoteId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNote_Code",
                schema: "tbl",
                table: "StockTransferNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNote_CreatedOnUtc",
                schema: "tbl",
                table: "StockTransferNote",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNote_FromWarehouseId_Status",
                schema: "tbl",
                table: "StockTransferNote",
                columns: new[] { "FromWarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferNoteItem_NoteId_ProductId",
                schema: "tbl",
                table: "StockTransferNoteItem",
                columns: new[] { "NoteId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                schema: "tbl",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_Code",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_SourceGoodsReceiptId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "SourceGoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_SourcePurchaseOrderId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "SourcePurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_SourceReturnId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNote_VendorId",
                schema: "tbl",
                table: "VendorCreditNote",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNoteAllocation_SourceReturnId",
                schema: "tbl",
                table: "VendorCreditNoteAllocation",
                column: "SourceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNoteAllocation_VendorCreditNoteId",
                schema: "tbl",
                table: "VendorCreditNoteAllocation",
                column: "VendorCreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditNoteAllocation_VendorDebtId",
                schema: "tbl",
                table: "VendorCreditNoteAllocation",
                column: "VendorDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_Code",
                schema: "tbl",
                table: "VendorDebt",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_GoodsReceiptId",
                schema: "tbl",
                table: "VendorDebt",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_PurchaseOrderId",
                schema: "tbl",
                table: "VendorDebt",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebt_VendorId",
                schema: "tbl",
                table: "VendorDebt",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_Code",
                schema: "tbl",
                table: "VendorPayment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_PurchaseOrderId",
                schema: "tbl",
                table: "VendorPayment",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_VendorDebtId",
                schema: "tbl",
                table: "VendorPayment",
                column: "VendorDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_VendorId",
                schema: "tbl",
                table: "VendorPayment",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_Code",
                schema: "tbl",
                table: "VendorReturn",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_GoodsReceiptId_Status",
                schema: "tbl",
                table: "VendorReturn",
                columns: new[] { "GoodsReceiptId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_PurchaseOrderId_Status",
                schema: "tbl",
                table: "VendorReturn",
                columns: new[] { "PurchaseOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturn_VendorId_Status",
                schema: "tbl",
                table: "VendorReturn",
                columns: new[] { "VendorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorReturnItem_VendorReturnId",
                schema: "tbl",
                table: "VendorReturnItem",
                column: "VendorReturnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankTransferPaymentIntent",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerCreditNoteAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerDebt",
                schema: "tbl");

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
                name: "CustomerPayment",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPaymentIntent",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPortalAccount",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPortalNotification",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPortalSession",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerPortalSettings",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerRefund",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturnItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturnRequestItemPicture",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerSecurityEvent",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "DeliveryNoteAccessToken",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "DeliveryNoteItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "DirectShipAddressChangeLog",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "ExpenseBudgets",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Expenses",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostingPolicy",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostLayer",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostLedgerEntry",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryCostRebuildRun",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "InventoryStock",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "OrderItemChangeAudit",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "ProductCategory",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "ProductPicture",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "ProductPriceHistory",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "ProductReservationLedger",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "ProductVendor",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItemChangeAudit",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "RolePermission",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockAdjustmentNoteItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockAuditLog",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockMovementLog",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockTransferNoteItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "UnitMeasurement",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "User",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "UserRole",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorCreditNoteAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorDebt",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorPayment",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorReturnItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerCreditNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerOrderRequest",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturn",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturnRequestItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "DeliveryNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItemAllocation",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "GoodsReceipt",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Warehouse",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Picture",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Vendor",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Permission",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockAdjustmentNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "StockTransferNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorCreditNote",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "VendorReturn",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "CustomerReturnRequest",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "OrderItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItem",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Order",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Product",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "PurchaseOrder",
                schema: "tbl");

            migrationBuilder.DropTable(
                name: "Customer",
                schema: "tbl");
        }
    }
}
