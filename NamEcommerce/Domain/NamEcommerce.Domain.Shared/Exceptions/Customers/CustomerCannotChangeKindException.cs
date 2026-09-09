namespace NamEcommerce.Domain.Shared.Exceptions.Customers;

[Serializable]
public sealed class CustomerCannotChangeKindException() : NamEcommerceDomainException("Error.SystemCustomerCannotChangeKind");
