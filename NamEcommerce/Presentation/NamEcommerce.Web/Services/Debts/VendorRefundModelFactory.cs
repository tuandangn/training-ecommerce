using MediatR;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Models.Debts;

namespace NamEcommerce.Web.Services.Debts;

public sealed class VendorRefundModelFactory(IMediator mediator) : IVendorRefundModelFactory
{
    public async Task<VendorRefundListSearchModel> PrepareRefundListSearchModel(VendorRefundListSearchModel? model = null)
    {
        model ??= new VendorRefundListSearchModel();

        var listQuery = new GetVendorRefundListQuery
        {
            VendorId = model.FilterVendorId,
            Status = model.FilterStatus,
            Keywords = model.FilterKeywords,
            PageNumber = model.PageNumber,
            PageSize = model.PageSize
        };

        model.Data = await mediator.Send(listQuery).ConfigureAwait(false);
        return model;
    }

    public async Task<VendorRefundDetailsViewModel?> PrepareRefundDetailsModel(Guid id)
    {
        var refund = await mediator.Send(new GetVendorRefundQuery { Id = id }).ConfigureAwait(false);
        if (refund is null) return null;

        var (statusLabel, badgeClass) = (VendorRefundStatus)refund.Status switch
        {
            VendorRefundStatus.Pending => ("Chờ thu tiền", "bg-warning text-dark"),
            VendorRefundStatus.Completed => ("Đã thu tiền", "bg-success"),
            VendorRefundStatus.Cancelled => ("Đã huỷ", "bg-secondary"),
            _ => ("Không xác định", "bg-light text-dark")
        };

        return new VendorRefundDetailsViewModel
        {
            Id = refund.Id,
            Code = refund.Code,
            VendorId = refund.VendorId,
            VendorName = refund.VendorName,
            VendorReturnId = refund.VendorReturnId,
            VendorReturnCode = refund.VendorReturnCode,
            Amount = refund.Amount,
            Status = (VendorRefundStatus)refund.Status,
            StatusLabel = statusLabel,
            StatusBadgeClass = badgeClass,
            PaymentMethod = (PaymentMethod?)refund.PaymentMethod,
            Note = refund.Note,
            RefundedOnUtc = refund.RefundedOnUtc,
            CreatedOnUtc = refund.CreatedOnUtc
        };
    }
}
