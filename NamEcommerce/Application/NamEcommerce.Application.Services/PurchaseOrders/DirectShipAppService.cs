using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.PurchaseOrders;

public sealed class DirectShipAppService(
    IDirectShipManager directShipManager,
    ICurrentUserAccessor currentUserAccessor) : IDirectShipAppService
{
    public async Task<CommonActionResultDto> MarkAllocationAsDirectShipAsync(MarkAllocationAsDirectShipAppDto dto)
    {
        try
        {
            await directShipManager.MarkAllocationAsDirectShipAsync(dto.AllocationId, dto.Address, dto.ContactName, dto.ContactPhone, dto.Priority)
                .ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<CommonActionResultDto> UpdateDirectShipAddressAsync(UpdateDirectShipAddressAppDto dto)
    {
        try
        {
            var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
            if (currentUser is null)
                return CommonActionResultDto.CreateError("Error.UserNotAuthenticated");

            await directShipManager.UpdateDirectShipAddressAsync(
                dto.AllocationId, dto.NewAddress, dto.NewContactName, dto.NewContactPhone,
                currentUser.Id, dto.Reason)
                .ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<CommonActionResultDto> ConfirmDeliveryAsync(ConfirmDirectShipDeliveryAppDto dto)
    {
        try
        {
            await directShipManager.ConfirmDeliveryAsync(
                dto.DeliveryNoteId, dto.ConfirmedAtUtc, dto.Note)
                .ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<CommonActionResultDto> RejectDeliveryAsync(RejectDirectShipDeliveryAppDto dto)
    {
        try
        {
            if (dto.ReturnWarehouseId == Guid.Empty)
                return CommonActionResultDto.CreateError("Vui lòng chọn kho nhận hàng trả về.");

            await directShipManager.RejectDeliveryAsync(dto.DeliveryNoteId, dto.ReturnWarehouseId, dto.Reason)
                .ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<IList<PendingDirectShipDeliveryAppDto>> GetPendingDeliveriesAsync(PendingDirectShipFilterAppDto filter)
    {
        var items = await directShipManager.GetPendingDeliveriesAsync(
            filter.Keywords, filter.FromDateUtc, filter.ToDateUtc)
            .ConfigureAwait(false);

        return items.Select(d => new PendingDirectShipDeliveryAppDto
        {
            Id = d.Id,
            Code = d.Code,
            OrderId = d.OrderId,
            OrderCode = d.OrderCode,
            CustomerName = d.CustomerName,
            CustomerPhone = d.CustomerPhone,
            ShippingAddress = d.ShippingAddress,
            CreatedOnUtc = d.CreatedOnUtc,
            Items = d.Items.Select(i => new PendingDirectShipDeliveryItemAppDto
            {
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        }).ToList();
    }

    public async Task<IList<DirectShipAllocationStatusAppDto>> GetDirectShipAllocationsForOrderAsync(
        IReadOnlyList<Guid> orderItemIds)
    {
        var items = await directShipManager.GetDirectShipAllocationsForOrderItemsAsync(orderItemIds)
            .ConfigureAwait(false);
        return items.Select(a => new DirectShipAllocationStatusAppDto
        {
            AllocationId = a.AllocationId,
            OrderItemId = a.OrderItemId,
            Status = a.Status,
            DeliveryStatus = a.DeliveryStatus,
            DeliveryNoteId = a.DeliveryNoteId,
            DeliveryNoteCode = a.DeliveryNoteCode,
            AllocatedQuantity = a.AllocatedQuantity,
            ReceivedQuantity = a.ReceivedQuantity
        }).ToList();
    }

    public async Task<IList<DirectShipAllocationForPoItemAppDto>> GetDirectShipAllocationsForPoItemsAsync(
        IReadOnlyList<Guid> purchaseOrderItemIds)
    {
        var items = await directShipManager.GetDirectShipAllocationsForPoItemsAsync(purchaseOrderItemIds)
            .ConfigureAwait(false);
        return items.Select(a => new DirectShipAllocationForPoItemAppDto
        {
            AllocationId = a.AllocationId,
            PurchaseOrderItemId = a.PurchaseOrderItemId,
            DirectShipAddress = a.DirectShipAddress,
            DirectShipContactName = a.DirectShipContactName,
            DirectShipContactPhone = a.DirectShipContactPhone,
            AllocatedQuantity = a.AllocatedQuantity,
            ReceivedQuantity = a.ReceivedQuantity,
            Status = a.Status,
            DeliveryStatus = a.DeliveryStatus,
            DeliveryNoteId = a.DeliveryNoteId,
            DeliveryNoteCode = a.DeliveryNoteCode
        }).ToList();
    }
}
