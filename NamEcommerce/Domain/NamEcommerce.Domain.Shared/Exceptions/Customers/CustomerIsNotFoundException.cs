namespace NamEcommerce.Domain.Shared.Exceptions.Customers;

[Serializable]
public sealed class CustomerIsNotFoundException(Guid id) : Exception($"Customer with id '{id}' is not found");
