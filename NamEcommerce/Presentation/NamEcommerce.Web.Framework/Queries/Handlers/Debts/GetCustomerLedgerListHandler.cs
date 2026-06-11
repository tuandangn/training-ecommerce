using MediatR;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerLedgerListHandler(ICustomerLedgerManager ledgerManager)
    : IRequestHandler<GetCustomerLedgerListQuery, CustomerLedgerListModel>
{
    public async Task<CustomerLedgerListModel> Handle(GetCustomerLedgerListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await ledgerManager.GetBalancesAsync(
            request.Keywords,
            request.PageIndex - 1,
            request.PageSize).ConfigureAwait(false);

        var items = pagedData.Items.Select(b => new CustomerDebtBalanceSummaryModel
        {
            CustomerId = b.CustomerId,
            CustomerName = b.CustomerName,
            CustomerPhone = b.CustomerPhone,
            Balance = b.Balance,
            LastEntryOnUtc = b.LastEntryOnUtc
        }).ToList();

        return new CustomerLedgerListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(items, pagedData.PagerInfo.PageIndex, pagedData.PagerInfo.PageSize, pagedData.PagerInfo.TotalCount)
        };
    }
}
