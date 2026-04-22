namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderLockedException()  : NamEcommerceDomainException("Error.OrderLocked");


