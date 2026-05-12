using MediatR;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Models.Debts;

namespace NamEcommerce.Web.Services.Debts;

public sealed class CustomerRefundModelFactory(IMediator mediator) : ICustomerRefundModelFactory
{
    public async Task<CustomerRefundListSearchModel> PrepareRefundListSearchModel(CustomerRefundListSearchModel? model = null)
    {
        model ??= new CustomerRefundListSearchModel();

        var listQuery = new GetCustomerRefundListQuery
        {
            CustomerId = model.FilterCustomerId,
            Status = model.FilterStatus,
            Keywords = model.FilterKeywords,
            PageNumber = model.PageNumber,
            PageSize = model.PageSize
        };

        model.Data = await mediator.Send(listQuery).ConfigureAwait(false);
        return model;
    }

    public async Task<CustomerRefundDetailsViewModel?> PrepareRefundDetailsModel(Guid id)
    {
        var refund = await mediator.Send(new GetCustomerRefundQuery { Id = id }).ConfigureAwait(false);
        if (refund is null) return null;

        var (statusLabel, badgeClass) = (CustomerRefundStatus)refund.Status switch
        {
            CustomerRefundStatus.Pending => ("Chờ hoàn tiền", "bg-warning text-dark"),
            CustomerRefundStatus.Completed => ("Đã hoàn tiền", "bg-success"),
            CustomerRefundStatus.Cancelled => ("Đã huỷ", "bg-secondary"),
            _ => ("Không xác định", "bg-light text-dark")
        };

        return new CustomerRefundDetailsViewModel
        {
            Id = refund.Id,
            Code = refund.Code,
            CustomerId = refund.CustomerId,
            CustomerName = refund.CustomerName,
            CustomerReturnId = refund.CustomerReturnId,
            CustomerReturnCode = refund.CustomerReturnCode,
            Amount = refund.Amount,
            Status = (CustomerRefundStatus)refund.Status,
            StatusLabel = statusLabel,
            StatusBadgeClass = badgeClass,
            PaymentMethod = (PaymentMethod?)refund.PaymentMethod,
            Note = refund.Note,
            RefundedOnUtc = refund.RefundedOnUtc,
            CreatedOnUtc = refund.CreatedOnUtc
        };
    }
}
