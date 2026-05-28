namespace NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

[Serializable]
public sealed record CustomerPortalAdminOverviewAppDto
{
    public IList<CustomerPortalAccountAdminAppDto> Accounts { get; init; } = [];
    public IList<CustomerPortalSecurityEventAdminAppDto> RecentSecurityEvents { get; init; } = [];
    public IList<CustomerPortalOrderRequestAdminAppDto> PendingOrderRequests { get; init; } = [];
    public IList<CustomerPortalReturnRequestAdminAppDto> PendingReturnRequests { get; init; } = [];
    public IList<CustomerPortalPaymentIntentAdminAppDto> PendingPaymentIntents { get; init; } = [];
}

[Serializable]
public sealed record CustomerPortalConversionResultAppDto
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public Guid? CreatedId { get; init; }

    public static CustomerPortalConversionResultAppDto Ok(Guid createdId, string? message = null)
        => new() { Success = true, CreatedId = createdId, Message = message };

    public static CustomerPortalConversionResultAppDto Fail(string? message = null)
        => new() { Success = false, Message = message };
}

[Serializable]
public sealed record CustomerPortalAccountAdminAppDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }
    public int Status { get; init; }
    public bool HasPassword { get; init; }
    public DateTime? PasswordSetOnUtc { get; init; }
    public DateTime? LastLoginOnUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerPortalSecurityEventAdminAppDto
{
    public required Guid Id { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public required string EventType { get; init; }
    public int Outcome { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? MetadataJson { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerPortalOrderRequestAdminAppDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required string Code { get; init; }
    public int Status { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; init; }
    public string? ShippingAddress { get; init; }
    public string? Note { get; init; }
    public string? AdminNote { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
    public Guid? ConvertedOrderId { get; init; }
    public decimal TotalAmount { get; init; }
    public bool RequiresPricing { get; init; }
    public IList<CustomerPortalOrderRequestItemAdminAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerPortalOrderRequestItemAdminAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPriceSnapshot { get; init; }
    public decimal SubTotal { get; init; }
    public bool RequiresPricing { get; init; }
}

[Serializable]
public sealed record CustomerPortalReturnRequestAdminAppDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public int Status { get; init; }
    public string? Reason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public string? AdminNote { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
    public Guid? ConvertedCustomerReturnId { get; init; }
    public IList<CustomerPortalReturnRequestItemAdminAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerPortalReturnRequestItemAdminAppDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal RequestedQuantity { get; init; }
    public decimal? OriginalUnitPrice { get; init; }
    public string? Reason { get; init; }
    public IList<CustomerPortalReturnRequestEvidencePictureAdminAppDto> EvidencePictures { get; init; } = [];
}

[Serializable]
public sealed record CustomerPortalReturnConversionItemAppDto
{
    public required Guid RequestItemId { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal ReturnUnitPrice { get; init; }
}

[Serializable]
public sealed record CustomerPortalReturnRequestEvidencePictureAdminAppDto
{
    public required Guid PictureId { get; init; }
    public string? PictureUrl { get; init; }
    public string? FileName { get; init; }
}

[Serializable]
public sealed record CustomerPortalPaymentIntentAdminAppDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public string? CustomerDebtCode { get; init; }
    public string? OrderCode { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public decimal Amount { get; init; }
    public required string Provider { get; init; }
    public string? ProviderIntentId { get; init; }
    public int Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? CompletedOnUtc { get; init; }
    public DateTime? ReconciledOnUtc { get; init; }
    public Guid? CustomerPaymentId { get; init; }
}
