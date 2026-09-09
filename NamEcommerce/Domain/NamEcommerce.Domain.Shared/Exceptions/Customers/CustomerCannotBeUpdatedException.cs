namespace NamEcommerce.Domain.Shared.Exceptions.Customers;

[Serializable]
public sealed class CustomerCannotBeUpdatedException() : NamEcommerceDomainException("Error.SystemCustomerCannotBeUpdated");
