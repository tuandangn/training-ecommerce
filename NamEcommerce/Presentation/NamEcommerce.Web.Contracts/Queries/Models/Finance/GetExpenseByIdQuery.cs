using NamEcommerce.Web.Contracts.Models.Finance;

namespace NamEcommerce.Web.Contracts.Queries.Models.Finance;

[Serializable]
public sealed record GetExpenseByIdQuery(Guid Id) : IRequest<ExpenseModel?>;
