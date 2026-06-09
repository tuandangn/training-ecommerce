using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetVendorRefundHandler(IVendorRefundAppService vendorRefundAppService)
    : IRequestHandler<GetVendorRefundQuery, VendorRefundModel?>
{
    public async Task<VendorRefundModel?> Handle(GetVendorRefundQuery request, CancellationToken cancellationToken)
    {
        var dto = await vendorRefundAppService.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (dto == null) return null;

        return new VendorRefundModel
        {
            Id = dto.Id,
            Code = dto.Code,
            VendorId = dto.VendorId,
            VendorName = dto.VendorName,
            VendorReturnId = dto.VendorReturnId,
            VendorReturnCode = dto.VendorReturnCode,
            VendorDebtId = dto.VendorDebtId,
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

public sealed class GetVendorRefundListHandler(IVendorRefundAppService vendorRefundAppService)
    : IRequestHandler<GetVendorRefundListQuery, VendorRefundListModel>
{
    public async Task<VendorRefundListModel> Handle(GetVendorRefundListQuery request, CancellationToken cancellationToken)
    {
        var pageIndex = request.PageNumber - 1;
        var paged = await vendorRefundAppService.GetListAsync(
            request.VendorId,
            request.Status,
            request.Keywords,
            pageIndex,
            request.PageSize).ConfigureAwait(false);

        var items = paged.Items.Select(dto => new VendorRefundModel
        {
            Id = dto.Id,
            Code = dto.Code,
            VendorId = dto.VendorId,
            VendorName = dto.VendorName,
            VendorReturnId = dto.VendorReturnId,
            VendorReturnCode = dto.VendorReturnCode,
            VendorDebtId = dto.VendorDebtId,
            Amount = dto.Amount,
            Status = dto.Status,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note,
            RefundedOnUtc = dto.RefundedOnUtc.HasValue ? DateTimeHelper.ToLocalTime(dto.RefundedOnUtc.Value) : null,
            CreatedOnUtc = DateTimeHelper.ToLocalTime(dto.CreatedOnUtc),
            UpdatedOnUtc = dto.UpdatedOnUtc.HasValue ? DateTimeHelper.ToLocalTime(dto.UpdatedOnUtc.Value) : null
        }).ToList();

        return new VendorRefundListModel
        {
            Items = items,
            TotalCount = paged.Pagination.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            FilterVendorId = request.VendorId,
            FilterStatus = request.Status,
            FilterKeywords = request.Keywords
        };
    }
}
