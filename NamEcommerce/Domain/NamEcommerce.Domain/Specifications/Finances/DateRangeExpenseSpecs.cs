using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.Finances;

[Serializable]
public sealed class DateRangeExpenseSpec(DateRangeFilter filter) : BaseSpecification<Expense>(
    new FromDateExpenseSpec(filter.FromDate).Criteria.And(
        new ToDateExpenseSpec(filter.ToDate).Criteria
));

[Serializable]
public sealed class FromDateExpenseSpec(DateTime? fromDate) : BaseSpecification<Expense>(
    expense => (fromDate == null || expense.IncurredDate >= fromDate));

[Serializable]
public sealed class ToDateExpenseSpec(DateTime? toDate) : BaseSpecification<Expense>(
    expense => (toDate == null || expense.IncurredDate <= toDate));
