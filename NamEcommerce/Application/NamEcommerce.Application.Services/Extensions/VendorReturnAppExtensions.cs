using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Application.Services.Extensions;

public static class VendorReturnAppExtensions
{
    public static VendorReturnAppDto ToAppDto(this VendorReturnDto dto)
        => new(dto.Id)
        {
            Code = dto.Code,
            VendorId = dto.VendorId,
            VendorName = dto.VendorName,
            PurchaseOrderId = dto.PurchaseOrderId,
            GoodsReceiptId = dto.GoodsReceiptId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ReturnDate = dto.ReturnDate,
            ConfirmedOnUtc = dto.ConfirmedOnUtc,
            GeneratedDeliveryNoteId = dto.GeneratedDeliveryNoteId,
            CreatedByUserId = dto.CreatedByUserId,
            CreatedOnUtc = dto.CreatedOnUtc,
            UpdatedOnUtc = dto.UpdatedOnUtc,
            Items = dto.Items.Select(i => new VendorReturnItemAppDto(i.Id)
            {
                VendorReturnId = i.VendorReturnId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                GoodsReceiptItemId = i.GoodsReceiptItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                UnitCost = i.UnitCost
            })
        };
}
