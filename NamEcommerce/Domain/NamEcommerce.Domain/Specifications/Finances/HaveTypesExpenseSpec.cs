using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Finances;

[Serializable]
public sealed class HaveTypesExpenseSpec(IList<ExpenseType> types) : BaseSpecification<Expense>(
    expense => types.Contains(expense.ExpenseType)
);
