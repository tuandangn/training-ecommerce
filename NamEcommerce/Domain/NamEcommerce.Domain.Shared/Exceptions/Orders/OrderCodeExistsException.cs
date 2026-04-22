namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderCodeExistsException(string code)  : NamEcommerceDomainException("Error.OrderCodeExists", code);



