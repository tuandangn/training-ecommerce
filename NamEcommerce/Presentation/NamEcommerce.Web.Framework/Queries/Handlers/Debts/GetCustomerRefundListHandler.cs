using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerRefundListHandler(ICustomerRefundAppService customerRefundAppService, AppConfig appConfig)
    : IRequestHandler<GetCustomerRefundListQuery, CustomerRefundListModel>
{
    public async Task<CustomerRefundListModel> Handle(GetCustomerRefundListQuery request, CancellationToken cancellationToken)
    {
        var pageIndex = request.PageNumber > 0 ? request.PageNumber - 1 : 0;
        var pageSize = request.PageSize > 0 ? request.PageSize : appConfig.DefaultPageSize;
        var paged = await customerRefundAppService.GetListAsync(pageIndex, pageSize, request.CustomerId,
            request.Status, request.Keywords).ConfigureAwait(false);

        var items = paged.Items.Select(dto => new CustomerRefundModel
        {
            Id = dto.Id,
            Code = dto.Code,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            CustomerReturnId = dto.CustomerReturnId,
            CustomerReturnCode = dto.CustomerReturnCode,
            CustomerDebtId = dto.CustomerDebtId,
            Amount = dto.Amount,
            Status = dto.Status,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note,
            RefundedOnUtc = dto.RefundedOnUtc.HasValue ? DateTimeHelper.ToLocalTime(dto.RefundedOnUtc.Value) : null,
            CreatedOnUtc = DateTimeHelper.ToLocalTime(dto.CreatedOnUtc),
            UpdatedOnUtc = dto.UpdatedOnUtc.HasValue ? DateTimeHelper.ToLocalTime(dto.UpdatedOnUtc.Value) : null
        }).ToList();

        return new CustomerRefundListModel
        {
            Items = PagedDataModel.Create(items, pageIndex, pageSize, paged.Pagination.TotalCount),
            FilterCustomerId = request.CustomerId,
            FilterStatus = request.Status,
            FilterKeywords = request.Keywords
        };
    }
}
