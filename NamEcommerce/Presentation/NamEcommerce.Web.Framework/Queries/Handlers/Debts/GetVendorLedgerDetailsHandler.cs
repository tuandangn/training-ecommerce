using MediatR;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetVendorLedgerDetailsHandler(IVendorLedgerManager ledgerManager)
    : IRequestHandler<GetVendorLedgerDetailsQuery, VendorLedgerDetailsModel?>
{
    public async Task<VendorLedgerDetailsModel?> Handle(GetVendorLedgerDetailsQuery request, CancellationToken cancellationToken)
    {
        var summary = await ledgerManager.GetVendorSummaryAsync(request.VendorId).ConfigureAwait(false);
        if (summary is null) return null;

        var statement = await ledgerManager.GetStatementAsync(
            request.VendorId,
            pageIndex: request.PageIndex - 1,
            pageSize: request.PageSize).ConfigureAwait(false);

        return new VendorLedgerDetailsModel
        {
            VendorId = summary.VendorId,
            VendorName = summary.VendorName,
            VendorPhone = summary.VendorPhone,
            Balance = summary.Balance,
            LastEntryOnUtc = summary.LastEntryOnUtc,
            Statement = statement.Items.Select(e => new VendorLedgerStatementEntryModel
            {
                EntryId = e.EntryId,
                EntryType = (int)e.EntryType,
                Amount = e.Amount,
                RunningBalance = e.RunningBalance,
                ReferenceType = (int)e.ReferenceType,
                ReferenceId = e.ReferenceId,
                ReferenceCode = e.ReferenceCode,
                Note = e.Note,
                OccurredAtUtc = e.OccurredAtUtc
            }).ToList(),
            PageIndex = request.PageIndex,
            TotalCount = statement.PagerInfo.TotalCount
        };
    }
}
