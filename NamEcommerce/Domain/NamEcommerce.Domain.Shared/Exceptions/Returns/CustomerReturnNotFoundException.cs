namespace NamEcommerce.Domain.Shared.Exceptions.Returns;

[Serializable]
public sealed class CustomerReturnNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.CustomerReturn.NotFound", id);
