using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using System.Net.NetworkInformation;

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
            }).ToList(),
            WarehousePicks = run.WarehousePicks.Select(pick => new DeliveryRunWarehousePickDto
            {
                WarehouseId = pick.WarehouseId,
                ConfirmedByUserId = pick.ConfirmedByUserId,
                ConfirmedByFullName = pick.ConfirmedByFullName,
                ConfirmedOnUtc = pick.ConfirmedOnUtc
            }).ToList(),
            CanIssuePaperManifest = run.Status == DeliveryRunStatus.ReadyForHandover && !run.PaperManifestIssued,
            CanCloseIfDelivered = run.Status == DeliveryRunStatus.HandedToDriver,
            CanCancel = run.Status is not DeliveryRunStatus.Closed and not DeliveryRunStatus.Cancelled,
            CanReviewCashCollected = !run.CashHandoverConfirmedOnUtc.HasValue && run.Status != DeliveryRunStatus.Cancelled,
            CanReconcileDeliveryRunItems = run.Status is not DeliveryRunStatus.Cancelled
        };
}
