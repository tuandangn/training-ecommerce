using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

namespace NamEcommerce.Domain.Services.Extensions;

public static class DeliveryRunExtensions
{
    public static DeliveryRunDto ToDto(this DeliveryRun run) =>
        new()
        {
            Id = run.Id,
            Code = run.Code,
            AssignedDeliveryUserId = run.AssignedDeliveryUserId,
            AssignedDeliveryUsername = run.AssignedDeliveryUsername,
            AssignedDeliveryFullName = run.AssignedDeliveryFullName,
            Status = run.Status,
            PreparedByUserId = run.PreparedByUserId,
            PreparedOnUtc = run.PreparedOnUtc,
            HandedOverByUserId = run.HandedOverByUserId,
            HandedOverOnUtc = run.HandedOverOnUtc,
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            DriverCacheDeviceId = run.DriverCacheDeviceId,
            PaperManifestIssued = run.PaperManifestIssued,
            PaperManifestIssuedOnUtc = run.PaperManifestIssuedOnUtc,
            CashHandoverConfirmedByUserId = run.CashHandoverConfirmedByUserId,
            CashHandoverConfirmedByUsername = run.CashHandoverConfirmedByUsername,
            CashHandoverConfirmedByFullName = run.CashHandoverConfirmedByFullName,
            CashHandoverConfirmedOnUtc = run.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = run.CashHandoverAmount,
            CashHandoverNote = run.CashHandoverNote,
            Note = run.Note,
            CreatedOnUtc = run.CreatedOnUtc,
            UpdatedOnUtc = run.UpdatedOnUtc,
            Items = run.Items.Select(item => new DeliveryRunItemDto
            {
                Id = item.Id,
                DeliveryRunId = item.DeliveryRunId,
                DeliveryNoteId = item.DeliveryNoteId,
                DeliveryNoteCode = item.DeliveryNoteCode,
                OrderCode = item.OrderCode,
                CustomerName = item.CustomerName,
                ShippingPhoneNumber = item.ShippingPhoneNumber,
                ShippingAddress = item.ShippingAddress,
                AmountToCollect = item.AmountToCollect
            }).ToList()
        };
}
