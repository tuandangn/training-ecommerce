namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderFulfillmentScheduleIsNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.OrderFulfillmentScheduleIsNotFound", id);
