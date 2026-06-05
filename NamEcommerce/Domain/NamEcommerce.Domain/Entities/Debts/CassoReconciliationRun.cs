using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CassoReconciliationRun : AppAggregateEntity
{
    private CassoReconciliationRun() : base(Guid.NewGuid()) { }

    internal CassoReconciliationRun(DateTime fromDate, DateTime toDate, CassoReconciliationRunTrigger trigger)
        : base(Guid.NewGuid())
    {
        if (fromDate > toDate)
            throw new NamEcommerceDomainException("Error.CassoReconciliationDateRangeInvalid");
        if (!Enum.IsDefined(trigger))
            throw new NamEcommerceDomainException("Error.CassoReconciliationTriggerInvalid");

        StartedAtUtc = DateTime.UtcNow;
        FromDate = fromDate;
        ToDate = toDate;
        Trigger = trigger;
    }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public DateTime FromDate { get; private set; }
    public DateTime ToDate { get; private set; }
    public CassoReconciliationRunTrigger Trigger { get; private set; }
    public int TotalRecords { get; private set; }
    public int Processed { get; private set; }
    public int Matched { get; private set; }
    public int Duplicate { get; private set; }
    public int Rejected { get; private set; }
    public int Ignored { get; private set; }
    public int Failed { get; private set; }
    public string? ErrorMessage { get; private set; }

    internal void Complete(
        int totalRecords,
        int processed,
        int matched,
        int duplicate,
        int rejected,
        int ignored,
        int failed,
        DateTime finishedAtUtc)
    {
        TotalRecords = totalRecords;
        Processed = processed;
        Matched = matched;
        Duplicate = duplicate;
        Rejected = rejected;
        Ignored = ignored;
        Failed = failed;
        FinishedAtUtc = finishedAtUtc;
    }

    internal void Fail(string errorMessage, DateTime finishedAtUtc)
    {
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Error.CassoReconciliationFailed" : errorMessage;
        Failed += 1;
        FinishedAtUtc = finishedAtUtc;
    }
}
