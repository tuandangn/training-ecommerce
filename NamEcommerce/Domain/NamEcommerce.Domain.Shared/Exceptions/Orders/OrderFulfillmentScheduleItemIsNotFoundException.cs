namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderFulfillmentScheduleItemIsNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.OrderFulfillmentScheduleItemIsNotFound", id);
