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
            await directShipManager.MarkAllocationAsDirectShipAsync(
                dto.AllocationId, dto.Address, dto.ContactName, dto.ContactPhone, dto.Priority)
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
            await directShipManager.RejectDeliveryAsync(dto.DeliveryNoteId, dto.Reason)
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
}
