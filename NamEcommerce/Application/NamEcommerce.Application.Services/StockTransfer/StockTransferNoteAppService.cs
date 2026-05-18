using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.StockTransfer;
using NamEcommerce.Application.Contracts.StockTransfer;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.StockTransfer;
using NamEcommerce.Domain.Shared.Enums.StockTransfer;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.StockTransfer;

namespace NamEcommerce.Application.Services.StockTransfer;

public sealed class StockTransferNoteAppService(IStockTransferNoteManager manager) : IStockTransferNoteAppService
{
    public async Task<CreateStockTransferNoteResultAppDto> CreateAsync(CreateStockTransferNoteAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid) return new CreateStockTransferNoteResultAppDto { Success = false, ErrorMessage = error };

        try
        {
            var domainDto = new CreateStockTransferNoteDto
            {
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Note = dto.Note,
                Items = dto.Items.Select(i => new CreateStockTransferNoteItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity
                }).ToList()
            };
            var result = await manager.CreateAsync(domainDto).ConfigureAwait(false);
            return new CreateStockTransferNoteResultAppDto { Success = true, CreatedId = result.Id };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new CreateStockTransferNoteResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<StockTransferNoteResultAppDto> ApproveAsync(Guid id)
    {
        try
        {
            await manager.ApproveAsync(id).ConfigureAwait(false);
            return new StockTransferNoteResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new StockTransferNoteResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<StockTransferNoteResultAppDto> CancelAsync(Guid id)
    {
        try
        {
            await manager.CancelAsync(id).ConfigureAwait(false);
            return new StockTransferNoteResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new StockTransferNoteResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<StockTransferNoteAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto?.ToAppDto();
    }

    public async Task<IPagedDataAppDto<StockTransferNoteListAppDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? fromWarehouseId, int? status)
    {
        var typedStatus = status.HasValue ? (StockTransferStatus?)status.Value : null;
        var paged = await manager.GetListAsync(pageIndex, pageSize, keywords, fromWarehouseId, typedStatus).ConfigureAwait(false);
        return PagedDataAppDto.Create(paged.Items.Select(x => x.ToListAppDto()).ToList(), pageIndex, pageSize, paged.PagerInfo.TotalCount);
    }
}
