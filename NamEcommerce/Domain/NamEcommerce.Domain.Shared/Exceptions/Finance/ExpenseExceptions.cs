using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Exceptions.Finance;

public sealed class ExpenseAmountCannotBeNegativeException() : NamEcommerceDomainException("Error.ExpenseAmountMustBePositive");

public sealed class ExpenseTitleRequiredException() : NamEcommerceDomainException("Error.ExpenseTitleRequired");

public sealed class ExpenseIsNotFoundException(Guid id) : NamEcommerceDomainException("Error.ExpenseIsNotFound", id);
