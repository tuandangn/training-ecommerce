using MediatR;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerLedgerDetailsHandler(ICustomerLedgerManager ledgerManager)
    : IRequestHandler<GetCustomerLedgerDetailsQuery, CustomerLedgerDetailsModel?>
{
    public async Task<CustomerLedgerDetailsModel?> Handle(GetCustomerLedgerDetailsQuery request, CancellationToken cancellationToken)
    {
        var summary = await ledgerManager.GetCustomerSummaryAsync(request.CustomerId).ConfigureAwait(false);
        if (summary is null) return null;

        var statement = await ledgerManager.GetStatementAsync(
            request.CustomerId,
            pageIndex: request.PageIndex - 1,
            pageSize: request.PageSize).ConfigureAwait(false);

        return new CustomerLedgerDetailsModel
        {
            CustomerId = summary.CustomerId,
            CustomerName = summary.CustomerName,
            CustomerPhone = summary.CustomerPhone,
            Balance = summary.Balance,
            LastEntryOnUtc = summary.LastEntryOnUtc,
            Statement = statement.Items.Select(e => new CustomerLedgerStatementEntryModel
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
