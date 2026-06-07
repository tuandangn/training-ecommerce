using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

namespace NamEcommerce.Application.Services.Extensions;

public static class DeliveryRunExtensions
{
    public static DeliveryRunAppDto ToAppDto(this DeliveryRunDto dto) =>
        new()
        {
            Id = dto.Id,
            Code = dto.Code,
            AssignedDeliveryUserId = dto.AssignedDeliveryUserId,
            AssignedDeliveryUsername = dto.AssignedDeliveryUsername,
            AssignedDeliveryFullName = dto.AssignedDeliveryFullName,
            Status = (int)dto.Status,
            PreparedByUserId = dto.PreparedByUserId,
            PreparedOnUtc = dto.PreparedOnUtc,
            HandedOverByUserId = dto.HandedOverByUserId,
            HandedOverOnUtc = dto.HandedOverOnUtc,
            DriverCachedOnUtc = dto.DriverCachedOnUtc,
            DriverCacheDeviceId = dto.DriverCacheDeviceId,
            PaperManifestIssued = dto.PaperManifestIssued,
            PaperManifestIssuedOnUtc = dto.PaperManifestIssuedOnUtc,
            CashHandoverConfirmedByUserId = dto.CashHandoverConfirmedByUserId,
            CashHandoverConfirmedByUsername = dto.CashHandoverConfirmedByUsername,
            CashHandoverConfirmedByFullName = dto.CashHandoverConfirmedByFullName,
            CashHandoverConfirmedOnUtc = dto.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = dto.CashHandoverAmount,
            CashHandoverNote = dto.CashHandoverNote,
            Note = dto.Note,
            CreatedOnUtc = dto.CreatedOnUtc,
            UpdatedOnUtc = dto.UpdatedOnUtc,
            Items = dto.Items.Select(item => new DeliveryRunItemAppDto
            {
                Id = item.Id,
                DeliveryNoteId = item.DeliveryNoteId,
                DeliveryNoteCode = item.DeliveryNoteCode,
                OrderCode = item.OrderCode,
                CustomerName = item.CustomerName,
                ShippingAddress = item.ShippingAddress,
                AmountToCollect = item.AmountToCollect
            }).ToList()
        };

    public static DeliveryRunListAppDto ToListAppDto(this DeliveryRunDto dto) =>
        new()
        {
            Id = dto.Id,
            Code = dto.Code,
            AssignedDeliveryUserId = dto.AssignedDeliveryUserId,
            AssignedDeliveryFullName = dto.AssignedDeliveryFullName,
            Status = (int)dto.Status,
            ItemCount = dto.Items.Count,
            AmountToCollect = dto.Items.Sum(item => item.AmountToCollect),
            DriverCachedOnUtc = dto.DriverCachedOnUtc,
            PaperManifestIssued = dto.PaperManifestIssued,
            HandedOverOnUtc = dto.HandedOverOnUtc,
            CashHandoverConfirmedOnUtc = dto.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = dto.CashHandoverAmount,
            CreatedOnUtc = dto.CreatedOnUtc
        };
}
