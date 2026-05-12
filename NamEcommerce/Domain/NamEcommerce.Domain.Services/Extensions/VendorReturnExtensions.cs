using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Domain.Services.Extensions;

public static class VendorReturnExtensions
{
    public static VendorReturnDto ToDto(this VendorReturn vendorReturn)
        => new(vendorReturn.Id)
        {
            Code = vendorReturn.Code,
            VendorId = vendorReturn.VendorId,
            VendorName = vendorReturn.VendorName,
            PurchaseOrderId = vendorReturn.PurchaseOrderId,
            GoodsReceiptId = vendorReturn.GoodsReceiptId,
            WarehouseId = vendorReturn.WarehouseId,
            WarehouseName = vendorReturn.WarehouseName,
            Note = vendorReturn.Note,
            Status = (int)vendorReturn.Status,
            ReturnDate = vendorReturn.ReturnDate,
            ConfirmedOnUtc = vendorReturn.ConfirmedOnUtc,
            ReversedOnUtc = vendorReturn.ReversedOnUtc,
            ReversedReason = vendorReturn.ReversedReason,
            AdditionalCost = vendorReturn.AdditionalCost,
            GeneratedDeliveryNoteId = vendorReturn.GeneratedDeliveryNoteId,
            CreatedByUserId = vendorReturn.CreatedByUserId,
            CreatedOnUtc = vendorReturn.CreatedOnUtc,
            UpdatedOnUtc = vendorReturn.UpdatedOnUtc,
            Items = vendorReturn.Items.Select(i => new VendorReturnItemDto(i.Id)
            {
                VendorReturnId = i.VendorReturnId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                GoodsReceiptItemId = i.GoodsReceiptItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                OriginalUnitCost = i.OriginalUnitCost,
                ReturnUnitCost = i.ReturnUnitCost
            })
        };
}
