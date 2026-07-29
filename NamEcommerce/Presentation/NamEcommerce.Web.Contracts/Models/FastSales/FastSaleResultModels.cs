namespace NamEcommerce.Web.Contracts.Models.FastSales;

[Serializable]
public sealed record QuickSaleOrderItemResultModel
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record QuickCreateOrderResultModel : ICommandResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? OrderId { get; init; }
    public string? OrderCode { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal OrderSubTotal { get; set; }
    public decimal OrderDiscount { get; set; }
}

[Serializable]
public sealed record QuickSaleResultModel : ICommandResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
    public Guid? PaymentIntentId { get; init; }
    public IList<QuickSaleOrderItemResultModel> OrderItems { get; init; } = [];
}

[Serializable]
public sealed record BankTransferPaymentIntentModel
{
    public required Guid Id { get; init; }
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string AccountName { get; init; }
    public required string QrImageUrl { get; init; }
    public required int Status { get; set; }
    public DateTime ExpiresAtUtc { get; init; }
    public int? VerificationSource { get; init; }
    public DateTime? VerifiedAtUtc { get; init; }

    public bool IsPending { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsExpired { get; set; }
    public bool IsCancelled { get; set; }
}

[Serializable]
public sealed record BankTransferPaymentIntentResultModel
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BankTransferPaymentIntentModel? Intent { get; init; }
    public Guid? VerificationLogId { get; init; }
}
