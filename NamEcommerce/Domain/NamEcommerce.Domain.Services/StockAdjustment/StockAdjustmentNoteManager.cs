using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.StockAdjustment;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.StockAdjustment;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;
using NamEcommerce.Domain.Shared.Exceptions.StockAdjustment;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.StockAdjustment;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.StockAdjustment;

public sealed class StockAdjustmentNoteManager(
    IRepository<StockAdjustmentNote> noteRepository,
    IEntityDataReader<StockAdjustmentNote> noteDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IEntityDataReader<InventoryStock> stockDataReader,
    ICurrentUserAccessor currentUserAccessor,
    EntityCodeGenerator entityCodeGenerator,
    IDbContext dbContext,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager) : IStockAdjustmentNoteManager
{
    private string GenerateCode()
    {
        var prefix = $"PKK-{DateTime.UtcNow:yyyyMMdd}";
        return entityCodeGenerator.Next(prefix, () => noteDataReader.DataSource.Count(n => n.Code.StartsWith(prefix)));
    }

    public async Task<StockAdjustmentNoteDto> CreateAsync(CreateStockAdjustmentNoteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId, default).ConfigureAwait(false);
        if (warehouse is null)
            throw new Shared.Exceptions.NamEcommerceDomainException("Error.StockAdjustment.WarehouseNotFound");

        var code = GenerateCode();
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var note = new StockAdjustmentNote(code, dto.WarehouseId, warehouse.Name, dto.Note, currentUser?.Id);

        foreach (var itemDto in dto.Items)
        {
            var product = await productDataReader.GetByIdAsync(itemDto.ProductId, default).ConfigureAwait(false);
            if (product is null)
                throw new Shared.Exceptions.NamEcommerceDomainException($"Error.StockAdjustment.ProductNotFound");

            note.AddItem(itemDto.ProductId, product.Name, itemDto.SystemQuantity, itemDto.PhysicalQuantity);
        }

        var inserted = await noteRepository.InsertAsync(note).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task ApproveAsync(Guid id)
    {
        var note = await noteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null) throw new StockAdjustmentNoteNotFoundException(id);

        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);

        var occurredAtUtc = DateTime.UtcNow;
        foreach (var item in note.Items.Where(i => i.Delta != 0))
        {
            await stockManager.ApplyAdjustmentAsync(
                item.ProductId,
                note.WarehouseId,
                item.Delta,
                note.Id,
                note.CreatedByUserId).ConfigureAwait(false);

            if (item.Delta > 0)
            {
                await inventoryCostingManager.RegisterInboundAsync(new RegisterInventoryInboundCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = note.WarehouseId,
                    Quantity = item.Delta,
                    UnitCost = null,
                    MovementType = InventoryCostMovementType.PositiveAdjustment,
                    ReferenceType = InventoryCostReferenceType.Adjustment,
                    ReferenceId = note.Id,
                    ReferenceItemId = item.Id,
                    OccurredAtUtc = occurredAtUtc
                }).ConfigureAwait(false);
            }
            else
            {
                await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = note.WarehouseId,
                    Quantity = Math.Abs(item.Delta),
                    MovementType = InventoryCostMovementType.NegativeAdjustment,
                    ReferenceType = InventoryCostReferenceType.Adjustment,
                    ReferenceId = note.Id,
                    ReferenceItemId = item.Id,
                    OccurredAtUtc = occurredAtUtc
                }).ConfigureAwait(false);
            }
        }

        note.Approve();
        await noteRepository.UpdateAsync(note).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var note = await noteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null) throw new StockAdjustmentNoteNotFoundException(id);

        note.Cancel();
        await noteRepository.UpdateAsync(note).ConfigureAwait(false);
    }

    public async Task<StockAdjustmentNoteDto?> GetByIdAsync(Guid id)
    {
        var note = await noteDataReader.GetByIdAsync(id, default).ConfigureAwait(false);
        return note?.ToDto();
    }

    public Task<IPagedDataDto<StockAdjustmentNoteDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? warehouseId, StockAdjustmentStatus? status)
    {
        var query = noteDataReader.DataSource;

        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(n => n.Code.Contains(keywords));
        if (warehouseId.HasValue)
            query = query.Where(n => n.WarehouseId == warehouseId.Value);
        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        query = query.OrderByDescending(n => n.CreatedOnUtc);

        var total = query.Count();
        if (total == 0)
            return Task.FromResult(PagedDataDto.Create(new List<StockAdjustmentNoteDto>(), pageIndex, pageSize, 0));

        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedDataDto.Create(items.Select(n => n.ToDto()).ToList(), pageIndex, pageSize, total));
    }
}
