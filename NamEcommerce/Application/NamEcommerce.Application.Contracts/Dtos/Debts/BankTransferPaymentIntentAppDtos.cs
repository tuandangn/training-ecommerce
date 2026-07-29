namespace NamEcommerce.Application.Contracts.Dtos.Debts;

[Serializable]
public sealed record BankTransferReceivingAccountAppDto
{
    public Guid? BankAccountId { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string AccountName { get; init; }
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BankId)
        && !string.IsNullOrWhiteSpace(AccountNo)
        && !string.IsNullOrWhiteSpace(AccountName);
}

[Serializable]
public sealed record CreateBankTransferPaymentIntentAppDto
{
    public required decimal Amount { get; init; }
    public required Guid CustomerId { get; init; }
    public string? Note { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Amount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");
        if (Amount != decimal.Truncate(Amount))
            return (false, "Error.BankTransferAmountMustBeWholeNumber");
        if (CustomerId == Guid.Empty)
            return (false, "Error.CustomerRequired");

        return (true, null);
    }
}

[Serializable]
public sealed record BankTransferPaymentIntentAppDto(Guid Id)
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public Guid? CustomerId { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string AccountName { get; init; }
    public required string Template { get; init; }
    public required string QrImageUrl { get; init; }
    public required int Status { get; set; }
    public string? Note { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
    public int? VerificationSource { get; init; }
    public string? ProviderTransactionId { get; init; }
    public DateTime? VerifiedAtUtc { get; init; }
    public Guid? VerifiedByUserId { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime? ExpiredAtUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record BankTransferPaymentIntentResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BankTransferPaymentIntentAppDto? Intent { get; init; }

    public static BankTransferPaymentIntentResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };

    public static BankTransferPaymentIntentResultAppDto CreateSuccess(BankTransferPaymentIntentAppDto intent)
        => new() { Success = true, Intent = intent };
}

[Serializable]
public sealed record ManualConfirmBankTransferPaymentIntentAppDto
{
    public required Guid IntentId { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record ConsumeBankTransferPaymentIntentAppDto
{
    public required Guid IntentId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public required Guid CustomerPaymentId { get; init; }
}

[Serializable]
public sealed record ProviderConfirmBankTransferPaymentIntentAppDto
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required int Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}

[Serializable]
public sealed record ProcessBankTransferProviderTransactionAppDto
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required int Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}

[Serializable]
public sealed record BankTransferProviderProcessingResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BankTransferPaymentIntentAppDto? Intent { get; init; }
    public Guid? VerificationLogId { get; init; }
}

[Serializable]
public sealed record BankTransferVerificationRequestAppDto
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
}

[Serializable]
public sealed record BankTransferVerificationProviderResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ProviderTransactionId { get; init; }
    public string? RawPayload { get; init; }
}
