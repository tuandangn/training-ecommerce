using MediatR;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Finance;

public sealed class GetExpensesHandler(IExpenseAppService expenseAppService, IExpenseBudgetAppService budgetAppService)
    : IRequestHandler<GetExpensesQuery, ExpenseListModel>
{
    public async Task<ExpenseListModel> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var paged = await expenseAppService.GetExpensesAsync(
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            keywords: request.Keywords,
            fromDate: request.FromDate.HasValue ? DateTimeHelper.ToUniversalTime(request.FromDate) : null,
            toDate: request.ToDate.HasValue ? DateTimeHelper.ToUniversalTime(request.ToDate) : null,
            expenseType: request.ExpenseType,
            sortBy: request.SortBy,
            sortDesc: request.SortDesc).ConfigureAwait(false);

        var items = paged.Select(x => new ExpenseListModel.ExpenseItemModel
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            Amount = x.Amount,
            ExpenseType = x.ExpenseType,
            IncurredDate = DateTimeHelper.ToLocalTime(x.IncurredDateUtc),
            SourceOrderId = x.SourceOrderId,
            SourceCustomerReturnId = x.SourceCustomerReturnId,
            SourceVendorReturnId = x.SourceVendorReturnId,
            IsSystemGenerated = x.ExpenseType == 6 || x.SourceOrderId.HasValue
        }).ToList();

        return new ExpenseListModel
        {
            Keywords = request.Keywords,
            FromDate = request.FromDate?.ToString("yyyy-MM-dd"),
            ToDate = request.ToDate?.ToString("yyyy-MM-dd"),
            ExpenseType = request.ExpenseType,
            SortBy = request.SortBy,
            SortDesc = request.SortDesc,
            Data = PagedDataModel.Create(items, request.PageIndex, request.PageSize, paged.Pagination.TotalCount),
            TotalAmount = paged.Sum(x => x.Amount)
        };
    }
}
