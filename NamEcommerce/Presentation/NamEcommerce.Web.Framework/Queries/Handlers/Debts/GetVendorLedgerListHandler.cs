using MediatR;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetVendorLedgerListHandler(IVendorLedgerManager ledgerManager)
    : IRequestHandler<GetVendorLedgerListQuery, VendorLedgerListModel>
{
    public async Task<VendorLedgerListModel> Handle(GetVendorLedgerListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await ledgerManager.GetBalancesAsync(
            request.Keywords,
            request.PageIndex - 1,
            request.PageSize).ConfigureAwait(false);

        var items = pagedData.Items.Select(b => new VendorDebtBalanceSummaryModel
        {
            VendorId = b.VendorId,
            VendorName = b.VendorName,
            VendorPhone = b.VendorPhone,
            Balance = b.Balance,
            LastEntryOnUtc = b.LastEntryOnUtc
        }).ToList();

        return new VendorLedgerListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(items, pagedData.PagerInfo.PageIndex, pagedData.PagerInfo.PageSize, pagedData.PagerInfo.TotalCount)
        };
    }
}
