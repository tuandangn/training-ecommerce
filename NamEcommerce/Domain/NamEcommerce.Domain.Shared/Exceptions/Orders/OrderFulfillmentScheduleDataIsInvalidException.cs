namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderFulfillmentScheduleDataIsInvalidException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);
