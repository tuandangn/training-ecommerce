using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Finances;

[Serializable]
public sealed class ExpensesOfOrdersSpec(IList<Guid> orderIds) : BaseSpecification<Expense>(
    expense => expense.SourceOrderId.HasValue && orderIds.Contains(expense.SourceOrderId.Value));
