using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.DeliveryNotes;

public sealed class DeliveryNoteAppService : IDeliveryNoteAppService
{
    private readonly IDeliveryNoteManager _deliveryNoteManager;
    private readonly IWarehouseAppService _warehouseAppService;

    public DeliveryNoteAppService(IDeliveryNoteManager deliveryNoteManager, IWarehouseAppService warehouseAppService)
    {
        _deliveryNoteManager = deliveryNoteManager;
        _warehouseAppService = warehouseAppService;
    }

    public async Task<DeliveryNoteAppDto> CreateFromOrderAsync(CreateDeliveryNoteAppDto dto)
    {
        var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(dto.WarehouseId);
        if (warehouse is null)
            throw new Exception("Kho hàng không tồn tại"); //*TODO*

        var domainDto = new CreateDeliveryNoteDto
        {
            OrderId = dto.OrderId,
            ShippingAddress = dto.ShippingAddress,
            ShowPrice = dto.ShowPrice,
            Note = dto.Note,
            WarehouseId = dto.WarehouseId,
            WarehouseName = warehouse.Name,
            Surcharge = dto.Surcharge,
            SurchargeReason = dto.SurchargeReason,
            AmountToCollect = dto.AmountToCollect,
            Items = dto.Items.Select(i => new CreateDeliveryNoteItemDto
            {
                OrderItemId = i.OrderItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        var result = await _deliveryNoteManager.CreateFromOrderAsync(domainDto).ConfigureAwait(false);
        return result.ToDto();
    }

    public async Task CancelAsync(Guid id)
    {
        await _deliveryNoteManager.CancelAsync(id).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id)
    {
        await _deliveryNoteManager.ConfirmAsync(id).ConfigureAwait(false);
    }

    public async Task MarkDeliveringAsync(Guid id)
    {
        await _deliveryNoteManager.MarkDeliveringAsync(id).ConfigureAwait(false);
    }

    public async Task<MarkDeliveryNoteDeliveredResultAppDto> MarkDeliveredAsync(MarkDeliveryNoteDeliveredAppDto dto)
    {
        try
        {
            await _deliveryNoteManager.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredDto
            {
                DeliveryNoteId = dto.DeliveryNoteId,
                PictureId = dto.PictureId,
                ReceiverName = dto.ReceiverName
            }).ConfigureAwait(false);

            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = true
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
