using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Exceptions.Finance;

public sealed class BankAccountNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.BankAccount.NotFound", id);

public sealed class BankAccountIsDefaultException()
    : NamEcommerceDomainException("Error.BankAccount.CannotDeactivateDefault");

public sealed class BankAccountDataInvalidException(string message)
    : NamEcommerceDomainException(message);
