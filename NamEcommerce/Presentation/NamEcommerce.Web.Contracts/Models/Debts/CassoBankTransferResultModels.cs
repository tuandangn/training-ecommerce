namespace NamEcommerce.Web.Contracts.Models.Debts;

[Serializable]
public sealed record CassoTransactionProcessingResultModel
{
    public bool Success { get; init; }
    public bool Ignored { get; init; }
    public string? Message { get; init; }
    public string? ProviderTransactionId { get; init; }
    public Guid? VerificationLogId { get; init; }
}

[Serializable]
public sealed record CassoBankTransferProcessingResultModel
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RunId { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
    public IList<CassoTransactionProcessingResultModel> Results { get; init; } = [];
}
