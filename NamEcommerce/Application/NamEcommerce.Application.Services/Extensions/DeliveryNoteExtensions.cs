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
            AssignedDeliveryUserId = deliveryNote.AssignedDeliveryUserId,
            AssignedDeliveryUsername = deliveryNote.AssignedDeliveryUsername,
            AssignedDeliveryFullName = deliveryNote.AssignedDeliveryFullName,
            AssignedDeliveryOnUtc = deliveryNote.AssignedDeliveryOnUtc,
            CustomerName = deliveryNote.CustomerName,
            CustomerPhone = deliveryNote.CustomerPhone,
            CustomerAddress = deliveryNote.CustomerAddress,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShippingPhoneNumber = deliveryNote.ShippingPhoneNumber,
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = (int)deliveryNote.Status,
            SourceType = (int)deliveryNote.SourceType,
            IsDirectShip = deliveryNote.IsDirectShip,
            DeliveryConfirmationStatus = (int)deliveryNote.DeliveryConfirmationStatus,
            ConfirmedAtUtc = deliveryNote.ConfirmedAtUtc,
            ConfirmedNote = deliveryNote.ConfirmedNote,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            DeliveryProofPictureId = deliveryNote.DeliveryProofPictureId,
            DeliveryReceiverName = deliveryNote.DeliveryReceiverName,
            DeliveryLatitude = deliveryNote.DeliveryLatitude,
            DeliveryLongitude = deliveryNote.DeliveryLongitude,
            DeliveryLocationAddress = deliveryNote.DeliveryLocationAddress,
            DeliveryCompletionNote = deliveryNote.DeliveryCompletionNote,
            DeliveryCompletionSource = deliveryNote.DeliveryCompletionSource,
            DeliveryCompletionIdempotencyKey = deliveryNote.DeliveryCompletionIdempotencyKey,
            DeliveryCashCollectedAmount = deliveryNote.DeliveryCashCollectedAmount,
            CreatedByUserId = deliveryNote.CreatedByUserId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            UpdatedOnUtc = deliveryNote.UpdatedOnUtc,
            TotalAmount = deliveryNote.TotalAmount,
            Surcharge = deliveryNote.Surcharge,
            SurchargeReason = deliveryNote.SurchargeReason,
            AmountToCollect = deliveryNote.AmountToCollect,
            SettlementApproval = (int)deliveryNote.SettlementApproval,
            ProposedAmountToCollect = deliveryNote.ProposedAmountToCollect,
            ApprovedAmountToCollect = deliveryNote.ApprovedAmountToCollect,
            ApprovedAgreedCustomerCharge = deliveryNote.ApprovedAgreedCustomerCharge,
            ApprovedAgreedChargeReason = deliveryNote.ApprovedAgreedChargeReason,
            SettlementReason = deliveryNote.SettlementReason,
            SettlementAdminNote = deliveryNote.SettlementAdminNote,
            SettlementItems = deliveryNote.SettlementItems.Select(s => new DeliveryNoteSettlementItemAppDto
            {
                DeliveryNoteItemId = s.DeliveryNoteItemId,
                AcceptedQuantity = s.AcceptedQuantity,
                RejectedQuantity = s.RejectedQuantity,
                RejectReason = s.RejectReason
            }).ToList(),
            Items = deliveryNote.Items.Select(i => new DeliveryNoteItemAppDto
            {
                Id = i.Id,
                DeliveryNoteId = i.DeliveryNoteId,
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                WarehouseId = i.WarehouseId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                CostAtDispatch = i.CostAtDispatch
            }).ToList()
        };
    }
}
