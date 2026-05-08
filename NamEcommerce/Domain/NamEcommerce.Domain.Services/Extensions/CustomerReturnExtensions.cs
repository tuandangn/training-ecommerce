using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Domain.Services.Extensions;

public static class CustomerReturnExtensions
{
    public static CustomerReturnDto ToDto(this CustomerReturn customerReturn)
        => new(customerReturn.Id)
        {
            Code = customerReturn.Code,
            DeliveryNoteId = customerReturn.DeliveryNoteId,
            DeliveryNoteCode = customerReturn.DeliveryNoteCode,
            CustomerId = customerReturn.CustomerId,
            CustomerName = customerReturn.CustomerName,
            WarehouseId = customerReturn.WarehouseId,
            WarehouseName = customerReturn.WarehouseName,
            Note = customerReturn.Note,
            Status = (int)customerReturn.Status,
            ReturnDate = customerReturn.ReturnDate,
            ConfirmedOnUtc = customerReturn.ConfirmedOnUtc,
            AdditionalCost = customerReturn.AdditionalCost,
            GeneratedGoodsReceiptId = customerReturn.GeneratedGoodsReceiptId,
            CreatedByUserId = customerReturn.CreatedByUserId,
            CreatedOnUtc = customerReturn.CreatedOnUtc,
            UpdatedOnUtc = customerReturn.UpdatedOnUtc,
            Items = customerReturn.Items.Select(i => new CustomerReturnItemDto(i.Id)
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
