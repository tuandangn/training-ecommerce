using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetVendorDebtListHandler(IVendorDebtAppService debtAppService) : IRequestHandler<GetVendorDebtListQuery, VendorDebtListModel>
{
    private readonly IVendorDebtAppService _debtAppService = debtAppService;

    public async Task<VendorDebtListModel> Handle(GetVendorDebtListQuery request, CancellationToken cancellationToken)
    {
        var pageIndex0 = request.PageIndex - 1;

        var pagedData = await _debtAppService.GetVendorsWithDebtsAsync(
            request.Keywords,
            pageIndex0,
            request.PageSize).ConfigureAwait(false);

        var items = pagedData.Items.Select(s => new VendorDebtVendorSummaryModel
        {
            VendorId = s.VendorId,
            VendorName = s.VendorName,
            VendorPhone = s.VendorPhone,
            VendorAddress = s.VendorAddress,
            TotalDebtAmount = s.TotalDebtAmount,
            TotalPaidAmount = s.TotalPaidAmount,
            TotalRemainingAmount = s.TotalRemainingAmount,
            AdvanceBalance = s.AdvanceBalance,
            DebtCount = s.DebtCount
        }).ToList();

        return new VendorDebtListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(items, pagedData.Pagination.PageIndex, pagedData.Pagination.PageSize, pagedData.Pagination.TotalCount)
        };
    }
}
