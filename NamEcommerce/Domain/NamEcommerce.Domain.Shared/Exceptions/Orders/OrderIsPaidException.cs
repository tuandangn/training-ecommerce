namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderIsPaidException(Guid id)  : NamEcommerceDomainException("Error.OrderIsPaid", id);


