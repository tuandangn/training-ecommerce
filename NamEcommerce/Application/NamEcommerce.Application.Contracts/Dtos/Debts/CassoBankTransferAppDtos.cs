using System.Text.Json.Serialization;

namespace NamEcommerce.Application.Contracts.Dtos.Debts;

[Serializable]
public sealed record ProcessCassoWebhookAppDto
{
    public int? Error { get; init; }
    public IList<CassoTransactionAppDto> Data { get; init; } = [];
    public string RawPayload { get; init; } = string.Empty;
}

[Serializable]
public sealed record RunCassoReconciliationAppDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public required int Trigger { get; init; }
}

[Serializable]
public sealed record GetCassoTransactionsAppDto
{
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

[Serializable]
public sealed record CassoTransactionPageAppDto
{
    public IList<CassoTransactionAppDto> Records { get; init; } = [];
    public bool HasMore { get; init; }
}

[Serializable]
public sealed record CassoTransactionAppDto
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("tid")]
    public string? Tid { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("when")]
    public string? When { get; init; }

    [JsonPropertyName("bank_sub_acc_id")]
    public string? BankSubAccId { get; init; }

    [JsonPropertyName("subAccId")]
    public string? SubAccId { get; init; }

    [JsonPropertyName("bankSubAccId")]
    public string? BankSubAccIdCamel { get; init; }

    [JsonPropertyName("bankName")]
    public string? BankName { get; init; }

    [JsonPropertyName("bankAbbreviation")]
    public string? BankAbbreviation { get; init; }

    [JsonPropertyName("bankCodeName")]
    public string? BankCodeName { get; init; }
}

[Serializable]
public sealed record CassoMappedTransactionAppDto
{
    public bool CanProcess { get; init; }
    public string? IgnoreReason { get; init; }
    public ProcessBankTransferProviderTransactionAppDto? ProviderTransaction { get; init; }
}

[Serializable]
public sealed record CassoTransactionProcessingResultAppDto
{
    public required bool Success { get; init; }
    public required bool Ignored { get; init; }
    public string? Message { get; init; }
    public string? ProviderTransactionId { get; init; }
    public Guid? VerificationLogId { get; init; }
}

[Serializable]
public sealed record CassoBankTransferProcessingResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RunId { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
    public IList<CassoTransactionProcessingResultAppDto> Results { get; init; } = [];
}
