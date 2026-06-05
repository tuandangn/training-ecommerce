using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Exceptions.Finance;

public sealed class AccountingSetupAlreadyFinalizedException()
    : NamEcommerceDomainException("Error.Accounting.SetupAlreadyFinalized");

public sealed class AccountingSetupNotFoundException()
    : NamEcommerceDomainException("Error.Accounting.SetupNotFound");

public sealed class AccountingSetupDataInvalidException(string message)
    : NamEcommerceDomainException(message);
