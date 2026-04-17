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
        var pagedData = await _debtAppService.GetDebtsAsync(request.CustomerId, request.PageIndex - 1, request.PageSize).ConfigureAwait(false);

        var debtModels = pagedData.Items.Select(d => new CustomerDebtListItemModel
        {
            Id = d.Id,
            Code = d.Code,
            CustomerName = d.CustomerName,
            DeliveryNoteCode = d.DeliveryNoteCode,
            OrderCode = d.OrderCode,
            TotalAmount = d.TotalAmount,
            PaidAmount = d.PaidAmount,
            RemainingAmount = d.RemainingAmount,
            Status = d.Status,
            StatusName = d.Status.ToString(),
            DueDateUtc = d.DueDateUtc?.ToLocalTime(),
            CreatedOnUtc = d.CreatedOnUtc.ToLocalTime()
        }).ToList();

        return new CustomerDebtListModel
        {
            Data = PagedDataModel.Create(debtModels, pagedData.Pagination.PageIndex + 1, pagedData.Pagination.PageSize, pagedData.Pagination.TotalCount)
        };
    }
}
