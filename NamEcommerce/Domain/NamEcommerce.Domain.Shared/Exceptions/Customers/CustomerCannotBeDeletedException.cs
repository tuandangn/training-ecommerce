namespace NamEcommerce.Domain.Shared.Exceptions.Customers;

[Serializable]
public sealed class CustomerCannotBeDeletedException(Guid id)
    : NamEcommerceDomainException("Error.CustomerCannotBeDeleted", id);
