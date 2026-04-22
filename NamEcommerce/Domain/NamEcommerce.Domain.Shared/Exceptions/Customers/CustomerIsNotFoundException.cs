namespace NamEcommerce.Domain.Shared.Exceptions.Customers;

[Serializable]
public sealed class CustomerIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.CustomerIsNotFound", id);


