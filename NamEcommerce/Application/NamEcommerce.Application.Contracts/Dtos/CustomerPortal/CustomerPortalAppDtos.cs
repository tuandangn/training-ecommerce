namespace NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

[Serializable]
public sealed record CustomerActionResultAppDto
{
    public required bool Success { get; init; }
    public string? Message { get; init; }

    public static CustomerActionResultAppDto Ok(string? message = null) => new() { Success = true, Message = message };
    public static CustomerActionResultAppDto Fail(string? message = null) => new() { Success = false, Message = message };
}

[Serializable]
public sealed record CustomerPortalDeliveryAccessTokenAppDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required string Token { get; init; }
}

[Serializable]
public sealed record PublicDeliveryNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public string? OrderCode { get; init; }
    public required int Status { get; init; }
    public int DeliveryConfirmationStatus { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }
    public IList<PublicDeliveryNoteItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record PublicDeliveryNoteItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record CustomerSessionAppDto
{
    public required Guid SessionId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public bool HasPassword { get; init; }
    public DateTime ExpiresOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerPortalLoginResultAppDto
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public string? SessionToken { get; init; }
    public CustomerSessionAppDto? Session { get; init; }
}

[Serializable]
public sealed record CustomerOtpRequestAppDto
{
    public required string DeliveryToken { get; init; }
    public string? RequestedIp { get; init; }
    public string? RequestedUserAgent { get; init; }
}

[Serializable]
public sealed record CustomerOtpRequestResultAppDto
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public Guid? ChallengeId { get; init; }
    public string? MaskedDestination { get; init; }
    public string? MockOtp { get; init; }
}

[Serializable]
public sealed record CustomerOtpVerifyAppDto
{
    public required Guid ChallengeId { get; init; }
    public required string Otp { get; init; }
    public string? RequestedIp { get; init; }
    public string? RequestedUserAgent { get; init; }
}

[Serializable]
public sealed record CustomerPasswordLoginAppDto
{
    public required string Login { get; init; }
    public required string Password { get; init; }
    public string? RequestedIp { get; init; }
    public string? RequestedUserAgent { get; init; }
}

[Serializable]
public sealed record SetCustomerPasswordAppDto
{
    public required string Password { get; init; }
}

[Serializable]
public sealed record ChangeCustomerPasswordAppDto
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

[Serializable]
public sealed record CustomerDashboardAppDto
{
    public IList<CustomerOrderSummaryAppDto> RecentOrders { get; init; } = [];
    public IList<CustomerDeliveryNoteSummaryAppDto> RecentDeliveryNotes { get; init; } = [];
    public CustomerDebtSummaryPortalAppDto DebtSummary { get; init; } = new();
}

[Serializable]
public record CustomerOrderSummaryAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public int Status { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; init; }
}

[Serializable]
public sealed record CustomerOrderDetailsAppDto : CustomerOrderSummaryAppDto
{
    public string? ShippingAddress { get; init; }
    public string? Note { get; init; }
    public IList<CustomerOrderItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerOrderItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
}

[Serializable]
public record CustomerDeliveryNoteSummaryAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public string? OrderCode { get; init; }
    public int Status { get; init; }
    public int DeliveryConfirmationStatus { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerDeliveryNoteDetailsAppDto : CustomerDeliveryNoteSummaryAppDto
{
    public IList<CustomerDeliveryNoteItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerDeliveryNoteItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
    public decimal ReservedReturnQuantity { get; init; }
    public decimal PendingPortalReturnQuantity { get; init; }
    public decimal ReturnableQuantity { get; init; }
}

[Serializable]
public sealed record ConfirmCustomerDeliveryAcceptanceItemAppDto
{
    public required Guid DeliveryNoteItemId { get; init; }
    public decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public string? RejectReason { get; init; }
}

[Serializable]
public sealed record ConfirmCustomerDeliveryAcceptanceAppDto
{
    public decimal AgreedCustomerCharge { get; init; }
    public string? AgreedCustomerChargeReason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public IList<ConfirmCustomerDeliveryAcceptanceItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record ConfirmCustomerDeliveryNoteAppDto
{
    public string? ReceiverName { get; init; }
    public string? Note { get; init; }
    public ConfirmCustomerDeliveryAcceptanceAppDto? Acceptance { get; init; }
}

[Serializable]
public sealed record CreateCustomerDeliveryFeedbackAppDto
{
    public required Guid DeliveryNoteId { get; init; }
    public int? Rating { get; init; }
    public string? Message { get; init; }
}

[Serializable]
public sealed record CreateCustomerOrderRequestAppDto
{
    public DateTime? ExpectedShippingDateUtc { get; init; }
    public string? ShippingAddress { get; init; }
    public string? Note { get; init; }
    public IList<CreateCustomerOrderRequestItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CreateCustomerOrderRequestItemAppDto
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record CustomerOrderRequestAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public int Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public record CustomerOrderRequestSummaryAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public int Status { get; init; }
    public decimal? TotalAmount { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
    public Guid? ConvertedOrderId { get; init; }
    public bool CanConfirm { get; init; }
}

[Serializable]
public sealed record CustomerOrderRequestDetailsAppDto : CustomerOrderRequestSummaryAppDto
{
    public string? ShippingAddress { get; init; }
    public string? Note { get; init; }
    public string? AdminNote { get; init; }
    public IList<CustomerOrderRequestItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerOrderRequestItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? SubTotal { get; init; }
    public bool IsPriced { get; init; }
}

[Serializable]
public sealed record CustomerProductListAppDto
{
    public IList<CustomerProductAppDto> Items { get; init; } = [];
    public bool HasMore { get; init; }
    public int PageSize { get; init; }
}

[Serializable]
public sealed record CustomerProductAppDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? PictureUrl { get; init; }
    public decimal? UnitPrice { get; init; }
    public bool HasPurchased { get; init; }
}

[Serializable]
public sealed record CustomerProductCategoryListAppDto
{
    public IList<CustomerProductCategoryAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerProductCategoryAppDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid? ParentId { get; init; }
}

[Serializable]
public sealed record CustomerOrderRequestDefaultsAppDto
{
    public string? ShippingAddress { get; init; }
    public string? ShippingAddressSource { get; init; }
}

[Serializable]
public sealed record CreateCustomerReturnRequestAppDto
{
    public Guid? DeliveryNoteId { get; init; }
    public string? Reason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public IList<CreateCustomerReturnRequestItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CreateCustomerReturnRequestItemAppDto
{
    public Guid? DeliveryNoteItemId { get; init; }
    public Guid? ProductId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public string? Reason { get; init; }
    public IList<CreateCustomerReturnRequestPictureAppDto> EvidencePictures { get; init; } = [];
}

[Serializable]
public sealed record CustomerReturnableItemAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string Unit { get; init; }
    public decimal DeliveredQuantity { get; init; }
    public decimal ReservedReturnQuantity { get; init; }
    public decimal ReturnableQuantity { get; init; }
    public decimal LatestUnitPrice { get; init; }
}

[Serializable]
public sealed record CreateCustomerReturnRequestPictureAppDto
{
    public required string FileName { get; init; }
    public required string MimeType { get; init; }
    public required string Base64Data { get; init; }
}

[Serializable]
public sealed record CustomerReturnRequestAppDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public int Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public bool CompensateInNextDelivery { get; init; }
}

[Serializable]
public record CustomerReturnRequestSummaryAppDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public int Status { get; init; }
    public string? Reason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public string? AdminNote { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
    public Guid? ConvertedCustomerReturnId { get; init; }
    public decimal TotalRequestedQuantity { get; init; }
    public int ItemCount { get; init; }
}

[Serializable]
public sealed record CustomerReturnRequestDetailsAppDto : CustomerReturnRequestSummaryAppDto
{
    public IList<CustomerReturnRequestItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerReturnRequestItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal RequestedQuantity { get; init; }
    public string? Reason { get; init; }
    public IList<CustomerReturnRequestEvidencePictureAppDto> EvidencePictures { get; init; } = [];
}

[Serializable]
public sealed record CustomerReturnRequestEvidencePictureAppDto
{
    public required Guid PictureId { get; init; }
    public string? PictureUrl { get; init; }
    public string? FileName { get; init; }
}

[Serializable]
public sealed record CustomerDebtSummaryPortalAppDto
{
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    public decimal DepositBalance { get; init; }
    public IList<CustomerDebtPortalAppDto> Debts { get; init; } = [];
    public IList<CustomerPaymentPortalAppDto> RecentPayments { get; init; } = [];
}

[Serializable]
public sealed record CustomerDebtPortalAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public string? OrderCode { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public int Status { get; init; }
    public DateTime? DueDateUtc { get; init; }
}

[Serializable]
public sealed record CustomerPaymentPortalAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public DateTime PaidOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerOtpSendAppDto
{
    public required int Channel { get; init; }
    public required string Destination { get; init; }
    public required string Otp { get; init; }
}

[Serializable]
public sealed record CustomerOtpSendResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record CustomerPortalNotificationSendAppDto
{
    public required int Channel { get; init; }
    public required string Destination { get; init; }
    public string? Subject { get; init; }
    public required string Message { get; init; }
}

[Serializable]
public sealed record CustomerPortalNotificationSendResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record CreateCustomerPaymentProviderIntentAppDto
{
    public required Guid PaymentIntentId { get; init; }
    public required decimal Amount { get; init; }
}

[Serializable]
public sealed record CreateCustomerPaymentProviderIntentResultAppDto
{
    public required bool Success { get; init; }
    public string? ProviderIntentId { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record CustomerPaymentProviderResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record CreateCustomerPaymentIntentAppDto
{
    public Guid? CustomerDebtId { get; init; }
    public required decimal Amount { get; init; }
}

[Serializable]
public sealed record CustomerPaymentIntentAppDto
{
    public required Guid Id { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public decimal Amount { get; init; }
    public required string Provider { get; init; }
    public string? ProviderIntentId { get; init; }
    public int Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? CompletedOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerContactAppDto
{
    public required CustomerStoreContactAppDto Store { get; init; }
    public IList<CustomerWarehouseContactAppDto> Warehouses { get; init; } = [];
}

[Serializable]
public sealed record CustomerStoreContactAppDto
{
    public required string StoreName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Email { get; init; }
    public string? MapQuery { get; init; }
}

[Serializable]
public sealed record CustomerWarehouseContactAppDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? MapQuery { get; init; }
}
