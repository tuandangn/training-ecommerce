using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerRefundHandler(ICustomerRefundAppService customerRefundAppService)
    : IRequestHandler<GetCustomerRefundQuery, CustomerRefundModel?>
{
    public async Task<CustomerRefundModel?> Handle(GetCustomerRefundQuery request, CancellationToken cancellationToken)
    {
        var dto = await customerRefundAppService.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (dto == null) return null;

        return new CustomerRefundModel
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
        };
    }
}

public sealed class GetCustomerRefundListHandler(ICustomerRefundAppService customerRefundAppService)
    : IRequestHandler<GetCustomerRefundListQuery, CustomerRefundListModel>
{
    public async Task<CustomerRefundListModel> Handle(GetCustomerRefundListQuery request, CancellationToken cancellationToken)
    {
        var pageIndex = request.PageNumber - 1;
        var paged = await customerRefundAppService.GetListAsync(
            request.CustomerId,
            request.Status,
            request.Keywords,
            pageIndex,
            request.PageSize).ConfigureAwait(false);

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
            Items = items,
            TotalCount = paged.Pagination.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            FilterCustomerId = request.CustomerId,
            FilterStatus = request.Status,
            FilterKeywords = request.Keywords
        };
    }
}
