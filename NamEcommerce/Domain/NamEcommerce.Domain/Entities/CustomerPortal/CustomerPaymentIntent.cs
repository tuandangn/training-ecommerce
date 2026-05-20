using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerPaymentIntent : AppAggregateEntity
{
    private CustomerPaymentIntent() : base(Guid.NewGuid()) { }

    internal CustomerPaymentIntent(Guid customerId, Guid? customerDebtId, decimal amount, string provider) : base(Guid.NewGuid())
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        CustomerId = customerId;
        CustomerDebtId = customerDebtId;
        Amount = amount;
        Provider = provider;
        Status = CustomerPaymentIntentStatus.Created;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public Guid? CustomerDebtId { get; private set; }
    public decimal Amount { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderIntentId { get; private set; }
    public CustomerPaymentIntentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }
    public DateTime? ReconciledOnUtc { get; private set; }
    public Guid? ReconciledByUserId { get; private set; }
    public Guid? CustomerPaymentId { get; private set; }

    internal void MarkProcessing(string providerIntentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerIntentId);

        ProviderIntentId = providerIntentId;
        Status = CustomerPaymentIntentStatus.Processing;
    }

    internal void MarkSucceededPendingReconciliation(DateTime nowUtc)
    {
        Status = CustomerPaymentIntentStatus.SucceededPendingReconciliation;
        CompletedOnUtc = nowUtc;
        FailureReason = null;
    }

    internal void MarkFailed(string? failureReason, DateTime nowUtc)
    {
        Status = CustomerPaymentIntentStatus.Failed;
        FailureReason = failureReason;
        CompletedOnUtc = nowUtc;
    }

    internal void Cancel()
    {
        if (Status is CustomerPaymentIntentStatus.Reconciled or CustomerPaymentIntentStatus.SucceededPendingReconciliation)
            throw new InvalidOperationException("Payment intent cannot be cancelled.");

        Status = CustomerPaymentIntentStatus.Cancelled;
    }

    internal void MarkReconciled(Guid customerPaymentId, Guid reconciledByUserId, DateTime nowUtc)
    {
        if (Status != CustomerPaymentIntentStatus.SucceededPendingReconciliation)
            throw new InvalidOperationException("Only successful pending payment intents can be reconciled.");

        CustomerPaymentId = customerPaymentId;
        ReconciledByUserId = reconciledByUserId;
        ReconciledOnUtc = nowUtc;
        Status = CustomerPaymentIntentStatus.Reconciled;
    }
}
