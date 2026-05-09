using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetVendorReturnListHandler : IRequestHandler<GetVendorReturnListQuery, VendorReturnListModel>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public GetVendorReturnListHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<VendorReturnListModel> Handle(GetVendorReturnListQuery request, CancellationToken cancellationToken)
    {
        var (total, items) = await _vendorReturnAppService.GetListAsync(
            request.VendorId,
            request.PurchaseOrderId,
            request.GoodsReceiptId,
            request.Status,
            request.PageIndex,
            request.PageSize
        ).ConfigureAwait(false);

        var itemModels = items.Select(dto => new VendorReturnListModel.ItemModel(dto.Id)
        {
            Code = dto.Code,
            VendorName = dto.VendorName,
            WarehouseName = dto.WarehouseName,
            Status = dto.Status,
            ReturnDate = DateTimeHelper.ToLocalTime(dto.ReturnDate),
            TotalAmount = dto.NetRecoveryAmount,
            ItemCount = dto.Items.Count()
        }).ToList();

        return new VendorReturnListModel
        {
            VendorId = request.VendorId,
            PurchaseOrderId = request.PurchaseOrderId,
            GoodsReceiptId = request.GoodsReceiptId,
            Status = request.Status,
            Data = PagedDataModel.Create(itemModels, request.PageIndex, request.PageSize, total)
        };
    }
}
