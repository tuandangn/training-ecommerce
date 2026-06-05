using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record StartCassoReconciliationRunDto
{
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required CassoReconciliationRunTrigger Trigger { get; init; }

    public void Verify()
    {
        if (FromDate > ToDate)
            throw new NamEcommerceDomainException("Error.CassoReconciliationDateRangeInvalid");
        if (!Enum.IsDefined(Trigger))
            throw new NamEcommerceDomainException("Error.CassoReconciliationTriggerInvalid");
    }
}

[Serializable]
public sealed record CompleteCassoReconciliationRunDto
{
    public required Guid RunId { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
}

[Serializable]
public sealed record FailCassoReconciliationRunDto
{
    public required Guid RunId { get; init; }
    public required string ErrorMessage { get; init; }
}

[Serializable]
public sealed record CassoReconciliationRunDto(Guid Id)
{
    public required DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required CassoReconciliationRunTrigger Trigger { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
    public string? ErrorMessage { get; init; }
}
