namespace NamEcommerce.Application.Contracts.Dtos.Debts;

[Serializable]
public sealed record VendorRefundAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required Guid VendorReturnId { get; init; }
    public required string VendorReturnCode { get; init; }
    public Guid? VendorDebtId { get; init; }
    public decimal Amount { get; init; }
    public int Status { get; init; }
    public int? PaymentMethod { get; init; }
    public Guid? BankAccountId { get; init; }
    public string? Note { get; init; }
    public DateTime? RefundedOnUtc { get; init; }
    public Guid? CompletedByUserId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record CompleteVendorRefundAppDto
{
    public required Guid RefundId { get; init; }
    public int PaymentMethod { get; init; }
    public Guid? BankAccountId { get; init; }
    public string? Note { get; init; }
    public Guid? CompletedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (RefundId == Guid.Empty)
            return (false, "Mã phiếu thu tiền hoàn không hợp lệ.");
        return (true, null);
    }
}

[Serializable]
public sealed record CompleteVendorRefundResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public VendorRefundAppDto? Refund { get; init; }
}

[Serializable]
public sealed record CancelVendorRefundResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
