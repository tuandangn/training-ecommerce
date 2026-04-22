namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.OrderIsNotFoundException", id);

