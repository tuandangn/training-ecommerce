namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class RetailOrderCannotLeaveDebtException : NamEcommerceDomainException
{
    public RetailOrderCannotLeaveDebtException()
        : base("Error.RetailOrderCannotLeaveDebt")
    {
    }
}
