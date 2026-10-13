using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Finances;

[Serializable]
public sealed class ExpensesOfOrdersSpec(IList<Guid> orderIds) : BaseSpecification<Expense>(
    expense => expense.ReferenceType == ExpenseReferenceType.Order && expense.ReferenceId.HasValue && orderIds.Contains(expense.ReferenceId.Value));
