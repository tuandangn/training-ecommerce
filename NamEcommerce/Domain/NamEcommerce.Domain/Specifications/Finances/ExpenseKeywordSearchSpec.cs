using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.Finances;

[Serializable]
public sealed class ExpenseKeywordSearchSpec(KeywordFilter filter) : BaseSpecification<Expense>(
    expense => expense.Title.ToUpper().Contains(filter.UppercaseKeywords) 
    || (expense.Description != null && expense.Description.ToUpper().Contains(filter.UppercaseKeywords)));
