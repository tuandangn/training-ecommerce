using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

namespace NamEcommerce.Application.Services.Extensions;

public static class DeliveryNoteExtensions
{
    public static DeliveryNoteAppDto ToDto(this DeliveryNoteDto source)
    {
        return new DeliveryNoteAppDto
        {
            Id = source.Id,
            Code = source.Code,
            OrderId = source.OrderId,
            CustomerId = source.CustomerId,
            CustomerName = source.CustomerName,
            CustomerPhone = source.CustomerPhone,
            CustomerAddress = source.CustomerAddress,
            ShippingAddress = source.ShippingAddress,
            ShowPrice = source.ShowPrice,
            Note = source.Note,
            Status = (int)source.Status,
            DeliveredOnUtc = source.DeliveredOnUtc,
            DeliveryProofPictureId = source.DeliveryProofPictureId,
            DeliveryReceiverName = source.DeliveryReceiverName,
            CreatedByUserId = source.CreatedByUserId,
            CreatedOnUtc = source.CreatedOnUtc,
            UpdatedOnUtc = source.UpdatedOnUtc,
            TotalAmount = source.TotalAmount,
            Items = source.Items.Select(i => new DeliveryNoteItemAppDto
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
