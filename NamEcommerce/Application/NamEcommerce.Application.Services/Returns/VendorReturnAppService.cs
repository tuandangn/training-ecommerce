using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Returns;

public sealed class VendorReturnAppService : IVendorReturnAppService
{
    private readonly IVendorReturnManager _manager;

    public VendorReturnAppService(IVendorReturnManager manager)
    {
        _manager = manager;
    }

    public async Task<CreateVendorReturnResultAppDto> CreateAsync(
        CreateVendorReturnAppDto dto, Guid? createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateVendorReturnResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = new CreateVendorReturnDto
            {
                VendorId = dto.VendorId,
                PurchaseOrderId = dto.PurchaseOrderId,
                GoodsReceiptId = dto.GoodsReceiptId,
                WarehouseId = dto.WarehouseId,
                Note = dto.Note,
                Items = dto.Items.Select(i => new CreateVendorReturnItemDto
                {
                    ProductId = i.ProductId,
                    GoodsReceiptItemId = i.GoodsReceiptItemId,
                    RequestedQuantity = i.RequestedQuantity,
                    AcceptedQuantity = i.AcceptedQuantity,
                    UnitCost = i.UnitCost
                })
            };

            var result = await _manager.CreateAsync(domainDto, createdByUserId).ConfigureAwait(false);
            return new CreateVendorReturnResultAppDto { Success = true, CreatedId = result.Id };
        }
        catch (ReturnDataIsInvalidException ex)
        {
            return new CreateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new CreateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<UpdateVendorReturnResultAppDto> UpdateAsync(UpdateVendorReturnAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var domainDto = new UpdateVendorReturnDto(dto.Id)
            {
                Note = dto.Note,
                ReturnDate = dto.ReturnDate
            };

            await _manager.UpdateAsync(domainDto).ConfigureAwait(false);
            return new UpdateVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new UpdateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new UpdateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> MoveToInspectingAsync(Guid id)
    {
        try
        {
            await _manager.MoveToInspectingAsync(id).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> ConfirmAsync(Guid id)
    {
        try
        {
            await _manager.ConfirmAsync(id).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ExceedsReceivedQuantityException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> CancelAsync(Guid id)
    {
        try
        {
            await _manager.CancelAsync(id).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<VendorReturnAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await _manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto?.ToAppDto();
    }

    public async Task<(int Total, List<VendorReturnAppDto> Items)> GetListAsync(
        Guid? vendorId, Guid? purchaseOrderId, Guid? goodsReceiptId, int? status, int pageIndex, int pageSize)
    {
        var (total, items) = await _manager.GetListAsync(
            vendorId, purchaseOrderId, goodsReceiptId, status, pageIndex, pageSize).ConfigureAwait(false);

        return (total, items.Select(i => i.ToAppDto()).ToList());
    }
}
