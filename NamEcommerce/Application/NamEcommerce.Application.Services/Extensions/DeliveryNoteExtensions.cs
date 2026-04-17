using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

namespace NamEcommerce.Application.Services.Extensions;

public static class DeliveryNoteExtensions
{
    public static DeliveryNoteAppDto ToDto(this DeliveryNoteDto deliveryNote)
    {
        return new DeliveryNoteAppDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderId = deliveryNote.OrderId,
            OrderCode = deliveryNote.OrderCode,
            CustomerId = deliveryNote.CustomerId,
            WarehouseId = deliveryNote.WarehouseId,
            CustomerName = deliveryNote.CustomerName,
            CustomerPhone = deliveryNote.CustomerPhone,
            CustomerAddress = deliveryNote.CustomerAddress,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = (int)deliveryNote.Status,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            DeliveryProofPictureId = deliveryNote.DeliveryProofPictureId,
            DeliveryReceiverName = deliveryNote.DeliveryReceiverName,
            CreatedByUserId = deliveryNote.CreatedByUserId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            UpdatedOnUtc = deliveryNote.UpdatedOnUtc,
            TotalAmount = deliveryNote.TotalAmount,
            Surcharge = deliveryNote.Surcharge,
            SurchargeReason = deliveryNote.SurchargeReason,
            AmountToCollect = deliveryNote.AmountToCollect,
            Items = deliveryNote.Items.Select(i => new DeliveryNoteItemAppDto
            {
                Id = i.Id,
                DeliveryNoteId = i.DeliveryNoteId,
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }
}
