using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetVendorReturnHandler : IRequestHandler<GetVendorReturnQuery, VendorReturnModel?>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public GetVendorReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<VendorReturnModel?> Handle(GetVendorReturnQuery request, CancellationToken cancellationToken)
    {
        var dto = await _vendorReturnAppService.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (dto is null)
            return null;

        var model = new VendorReturnModel
        {
            Id = dto.Id,
            Code = dto.Code,
            VendorId = dto.VendorId,
            VendorName = dto.VendorName,
            PurchaseOrderId = dto.PurchaseOrderId,
            GoodsReceiptId = dto.GoodsReceiptId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ReturnDate = DateTimeHelper.ToLocalTime(dto.ReturnDate),
            ConfirmedOn = DateTimeHelper.ToLocalTime(dto.ConfirmedOnUtc),
            AdditionalCost = dto.AdditionalCost,
            GeneratedDeliveryNoteId = dto.GeneratedDeliveryNoteId,
            CreatedOn = DateTimeHelper.ToLocalTime(dto.CreatedOnUtc),
            UpdatedOn = DateTimeHelper.ToLocalTime(dto.UpdatedOnUtc)
        };

        foreach (var item in dto.Items)
        {
            model.Items.Add(new VendorReturnModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                GoodsReceiptItemId = item.GoodsReceiptItemId,
                RequestedQuantity = item.RequestedQuantity,
                AcceptedQuantity = item.AcceptedQuantity,
                OriginalUnitCost = item.OriginalUnitCost,
                ReturnUnitCost = item.ReturnUnitCost
            });
        }

        return model;
    }
}
