using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.DeliveryNotes;

public sealed class DeliveryNoteAppService : IDeliveryNoteAppService
{
    private readonly IDeliveryNoteManager _deliveryNoteManager;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IUserAppService _userAppService;

    public DeliveryNoteAppService(
        IDeliveryNoteManager deliveryNoteManager,
        IWarehouseAppService warehouseAppService,
        IUserAppService userAppService)
    {
        _deliveryNoteManager = deliveryNoteManager;
        _warehouseAppService = warehouseAppService;
        _userAppService = userAppService;
    }

    public async Task<DeliveryNoteAppDto> CreateFromOrderAsync(CreateDeliveryNoteAppDto dto)
    {
        var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(dto.WarehouseId);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(dto.WarehouseId);

        foreach (var warehouseId in dto.Items.Select(item => item.WarehouseId).Distinct())
        {
            if (warehouseId == dto.WarehouseId)
                continue;

            if (await _warehouseAppService.GetWarehouseByIdAsync(warehouseId).ConfigureAwait(false) is null)
                throw new WarehouseIsNotFoundException(warehouseId);
        }

        var domainDto = new CreateDeliveryNoteDto
        {
            OrderId = dto.OrderId,
            ShippingAddress = dto.ShippingAddress,
            ShowPrice = dto.ShowPrice,
            CompensateReturnedQuantityInNextDelivery = dto.CompensateReturnedQuantityInNextDelivery,
            Note = dto.Note,
            WarehouseId = dto.WarehouseId,
            WarehouseName = warehouse.Name,
            Surcharge = dto.Surcharge,
            SurchargeReason = dto.SurchargeReason,
            AmountToCollect = dto.AmountToCollect,
            Items = dto.Items.Select(i => new CreateDeliveryNoteItemDto
            {
                OrderItemId = i.OrderItemId,
                WarehouseId = i.WarehouseId,
                Quantity = i.Quantity
            }).ToList()
        };

        var result = await _deliveryNoteManager.CreateFromOrderAsync(domainDto).ConfigureAwait(false);
        return result.ToDto();
    }

    public async Task<Guid> CreateAsDeliveredFromVendorReturnAsync(CreateDeliveryNoteFromVendorReturnAppDto dto)
    {
        var domainDto = new CreateDeliveryNoteFromVendorReturnDto
        {
            VendorReturnId = dto.VendorReturnId,
            WarehouseId = dto.WarehouseId,
            Items = dto.Items.Select(i => new CreateDeliveryNoteFromVendorReturnItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            })
        };

        return await _deliveryNoteManager.CreateAsDeliveredAsync(domainDto).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        await _deliveryNoteManager.CancelAsync(id).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id)
    {
        await _deliveryNoteManager.ConfirmAsync(id).ConfigureAwait(false);
    }

    public async Task<CommonActionResultDto> MarkDeliveringAsync(Guid id)
    {
        try
        {
            await _deliveryNoteManager.MarkDeliveringAsync(id).ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (NamEcommerceDomainException ex)
        {
            return CommonActionResultDto.CreateError(ex.ErrorCode);
        }
    }

    public async Task<MarkDeliveryNoteDeliveredResultAppDto> MarkDeliveredAsync(MarkDeliveryNoteDeliveredAppDto dto)
    {
        try
        {
            await _deliveryNoteManager.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredDto
            {
                DeliveryNoteId = dto.DeliveryNoteId,
                PictureIds = dto.PictureIds,
                ReceiverName = dto.ReceiverName,
                Acceptance = dto.Acceptance is null
                    ? null
                    : new DeliveryAcceptanceDto
                    {
                        AgreedCustomerCharge = dto.Acceptance.AgreedCustomerCharge,
                        AgreedCustomerChargeReason = dto.Acceptance.AgreedCustomerChargeReason,
                        CompensateInNextDelivery = dto.Acceptance.CompensateInNextDelivery,
                        Items = dto.Acceptance.Items.Select(item => new DeliveryAcceptanceItemDto
                        {
                            DeliveryNoteItemId = item.DeliveryNoteItemId,
                            AcceptedQuantity = item.AcceptedQuantity,
                            RejectedQuantity = item.RejectedQuantity,
                            RejectReason = item.RejectReason
                          }).ToList()
                      }
                ,
                CompletionMetadata = dto.CompletionMetadata is null
                    ? null
                    : new DeliveryCompletionMetadataDto
                    {
                        Latitude = dto.CompletionMetadata.Latitude,
                        Longitude = dto.CompletionMetadata.Longitude,
                        LocationAddress = dto.CompletionMetadata.LocationAddress,
                        Note = dto.CompletionMetadata.Note,
                        Source = dto.CompletionMetadata.Source,
                        IdempotencyKey = dto.CompletionMetadata.IdempotencyKey,
                        CashCollectedAmount = dto.CompletionMetadata.CashCollectedAmount
                    }
            }).ConfigureAwait(false);

            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = true
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = ex.ErrorCode
            };
        }
        catch (Exception ex)
        {
            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AssignDeliveryUserResultAppDto> AssignDeliveryUserAsync(AssignDeliveryUserAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var user = await _userAppService.GetUserByIdAsync(dto.AssignedDeliveryUserId).ConfigureAwait(false);
        if (user is null)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.UserNotFound"
            };
        }

        var isDeliveryStaff = await _userAppService
            .IsUserInRoleAsync(user.Id, SystemUserRoleNames.DeliveryStaff)
            .ConfigureAwait(false);
        if (!isDeliveryStaff)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.UserIsNotDeliveryStaff"
            };
        }

        try
        {
            await _deliveryNoteManager.AssignDeliveryUserAsync(new AssignDeliveryUserDto
            {
                DeliveryNoteId = dto.DeliveryNoteId,
                AssignedDeliveryUserId = user.Id,
                AssignedDeliveryUsername = user.Username,
                AssignedDeliveryFullName = user.FullName
            }).ConfigureAwait(false);

            return new AssignDeliveryUserResultAppDto
            {
                Success = true
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = ex.ErrorCode
            };
        }
    }

    public async Task<DeliveryNoteAppDto?> GetByIdAsync(Guid id)
    {
        var result = await _deliveryNoteManager.GetByIdAsync(id).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<IList<DeliveryNoteAppDto>> GetByOrderIdAsync(Guid orderId)
    {
        // For simplicity, we use GetListAsync and filter it down.
        // In a real app we might add a specific domain query.
        // Assuming page 0 to MAX gets enough for a single order's notes.
        var paged = await _deliveryNoteManager.GetDeliveryNotesAsync(0, int.MaxValue).ConfigureAwait(false);
        return paged.Where(d => d.OrderId == orderId)
                    .Select(d => d.ToDto())
                    .OrderBy(d => d.CreatedOnUtc)
                    .ToList();
    }

    public async Task<PagedDataAppDto<DeliveryNoteAppDto>> GetListAsync(string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var paged = await _deliveryNoteManager.GetDeliveryNotesAsync(pageIndex, pageSize, keywords).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(d => d.ToDto()).ToList();
        var result = PagedDataAppDto.Create(mappedItems, paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
        return (PagedDataAppDto<DeliveryNoteAppDto>)result;
    }
}
