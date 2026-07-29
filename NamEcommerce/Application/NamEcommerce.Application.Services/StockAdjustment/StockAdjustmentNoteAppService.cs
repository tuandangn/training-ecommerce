using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Dtos.StockAdjustment;
using NamEcommerce.Application.Contracts.StockAdjustment;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.StockAdjustment;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.StockAdjustment;

namespace NamEcommerce.Application.Services.StockAdjustment;

public sealed class StockAdjustmentNoteAppService(IStockAdjustmentNoteManager manager,
    IEntityDataReader<Product> productDataReader, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader) : IStockAdjustmentNoteAppService
{
    public async Task<CreateStockAdjustmentNoteResultAppDto> CreateAsync(CreateStockAdjustmentNoteAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid)
        {
            return new CreateStockAdjustmentNoteResultAppDto
            {
                Success = false,
                ErrorMessage = error
            };
        }

        foreach (var item in dto.Items)
        {

            var product = await productDataReader.GetByIdAsync(item.ProductId).ConfigureAwait(false);
            if (product is null)
            {
                return new CreateStockAdjustmentNoteResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.ProductIsNotFound"
                };
            }

            if (product.UnitMeasurementId.HasValue)
            {
                var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
                if (unitMeasurement is not null)
                {
                    if (!NumberHelper.IsValidDecimalPlace(item.SystemQuantity, unitMeasurement.DecimalPlaces) || !NumberHelper.IsValidDecimalPlace(item.PhysicalQuantity, unitMeasurement.DecimalPlaces))
                    {
                        return new CreateStockAdjustmentNoteResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }
        }

        try
        {
            var domainDto = new CreateStockAdjustmentNoteDto
            {
                WarehouseId = dto.WarehouseId,
                Note = dto.Note,
                Items = dto.Items.Select(i => new CreateStockAdjustmentNoteItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    SystemQuantity = i.SystemQuantity,
                    PhysicalQuantity = i.PhysicalQuantity
                }).ToList()
            };
            var result = await manager.CreateAsync(domainDto).ConfigureAwait(false);
            return new CreateStockAdjustmentNoteResultAppDto { Success = true, CreatedId = result.Id };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new CreateStockAdjustmentNoteResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<(bool Success, string? Error)> ApproveAsync(Guid id)
    {
        try
        {
            await manager.ApproveAsync(id).ConfigureAwait(false);
            return (true, null);
        }
        catch (NamEcommerceDomainException ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> CancelAsync(Guid id)
    {
        try
        {
            await manager.CancelAsync(id).ConfigureAwait(false);
            return (true, null);
        }
        catch (NamEcommerceDomainException ex) { return (false, ex.Message); }
    }

    public async Task<StockAdjustmentNoteAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto?.ToAppDto();
    }

    public async Task<IPagedDataAppDto<StockAdjustmentNoteListAppDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? warehouseId, int? status)
    {
        var paged = await manager.GetListAsync(pageIndex, pageSize, keywords, warehouseId, (StockAdjustmentStatus?)status).ConfigureAwait(false);
        return PagedDataAppDto.Create(paged.Items.Select(x => x.ToListAppDto()).ToList(), pageIndex, pageSize, paged.PagerInfo.TotalCount);
    }
}
