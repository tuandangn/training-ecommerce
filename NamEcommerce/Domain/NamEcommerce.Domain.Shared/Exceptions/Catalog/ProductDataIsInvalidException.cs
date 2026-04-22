namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.ProductDataIsInvalidException", message);
