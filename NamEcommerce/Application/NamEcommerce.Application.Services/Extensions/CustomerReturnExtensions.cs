using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Application.Services.Extensions;

public static class CustomerReturnAppExtensions
{
    public static CustomerReturnAppDto ToAppDto(this CustomerReturnDto dto)
        => new(dto.Id)
        {
            Code = dto.Code,
            DeliveryNoteId = dto.DeliveryNoteId,
            DeliveryNoteCode = dto.DeliveryNoteCode,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ReturnDate = dto.ReturnDate,
            ConfirmedOnUtc = dto.ConfirmedOnUtc,
            AdditionalCost = dto.AdditionalCost,
            CompensateInNextDelivery = dto.CompensateInNextDelivery,
            GeneratedGoodsReceiptId = dto.GeneratedGoodsReceiptId,
            CreatedByUserId = dto.CreatedByUserId,
            CreatedOnUtc = dto.CreatedOnUtc,
            UpdatedOnUtc = dto.UpdatedOnUtc,
            Items = dto.Items.Select(i => new CustomerReturnItemAppDto(i.Id)
            {
                CustomerReturnId = i.CustomerReturnId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                DeliveryNoteItemId = i.DeliveryNoteItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                OriginalUnitPrice = i.OriginalUnitPrice,
                ReturnUnitPrice = i.ReturnUnitPrice
            })
        };
}
