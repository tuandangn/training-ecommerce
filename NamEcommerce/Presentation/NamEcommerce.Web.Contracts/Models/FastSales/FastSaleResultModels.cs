namespace NamEcommerce.Web.Contracts.Models.FastSales;

[Serializable]
public sealed record QuickSaleResultModel
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
    public Guid? PaymentIntentId { get; init; }
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
    public required int Status { get; init; }
}

[Serializable]
public sealed record BankTransferPaymentIntentResultModel
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BankTransferPaymentIntentModel? Intent { get; init; }
}
