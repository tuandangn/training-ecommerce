using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerDebtListHandler(ICustomerDebtAppService debtAppService) : IRequestHandler<GetCustomerDebtListQuery, CustomerDebtListModel>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;

    public async Task<CustomerDebtListModel> Handle(GetCustomerDebtListQuery request, CancellationToken cancellationToken)
    {
        var pageIndex0 = request.PageIndex - 1;

        var pagedData = await _debtAppService.GetCustomersWithDebtsAsync(
            request.Keywords,
            pageIndex0,
            request.PageSize).ConfigureAwait(false);

        var items = pagedData.Items.Select(s => new CustomerDebtCustomerSummaryModel
        {
            CustomerId = s.CustomerId,
            CustomerName = s.CustomerName,
            CustomerPhone = s.CustomerPhone,
            CustomerAddress = s.CustomerAddress,
            TotalDebtAmount = s.TotalDebtAmount,
            TotalPaidAmount = s.TotalPaidAmount,
            TotalRemainingAmount = s.TotalRemainingAmount,
            DepositBalance = s.DepositBalance,
            DebtCount = s.DebtCount
        }).ToList();

        return new CustomerDebtListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(items, pagedData.Pagination.PageIndex, pagedData.Pagination.PageSize, pagedData.Pagination.TotalCount)
        };
    }
}
