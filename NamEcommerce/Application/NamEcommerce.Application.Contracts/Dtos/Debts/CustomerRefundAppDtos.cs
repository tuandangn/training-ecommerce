namespace NamEcommerce.Application.Contracts.Dtos.Debts;

[Serializable]
public sealed record CustomerRefundAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid CustomerReturnId { get; init; }
    public required string CustomerReturnCode { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public decimal Amount { get; init; }
    public int Status { get; init; }
    public int? PaymentMethod { get; init; }
    public string? Note { get; init; }
    public DateTime? RefundedOnUtc { get; init; }
    public Guid? CompletedByUserId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record CompleteCustomerRefundAppDto
{
    public required Guid RefundId { get; init; }
    public int PaymentMethod { get; init; }
    public string? Note { get; init; }
    public Guid? CompletedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (RefundId == Guid.Empty)
            return (false, "Mã phiếu hoàn tiền không hợp lệ.");
        return (true, null);
    }
}

[Serializable]
public sealed record CompleteCustomerRefundResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CustomerRefundAppDto? Refund { get; init; }
}

[Serializable]
public sealed record CancelCustomerRefundResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
