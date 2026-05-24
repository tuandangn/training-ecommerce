using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.StockTransfer;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.StockTransfer;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.StockTransfer;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.StockTransfer;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.StockTransfer;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.StockTransfer;

public sealed class StockTransferNoteManager(
    IRepository<StockTransferNote> noteRepository,
    IEntityDataReader<StockTransferNote> noteDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager,
    ICurrentUserAccessor currentUserAccessor) : IStockTransferNoteManager
{
    private string GenerateCode()
    {
        var prefix = $"PCT-{DateTime.UtcNow:yyyyMMdd}";
        var count = noteDataReader.DataSource.Count(n => n.Code.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D3}";
    }

    public async Task<StockTransferNoteDto> CreateAsync(CreateStockTransferNoteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var fromWarehouse = await warehouseDataReader.GetByIdAsync(dto.FromWarehouseId).ConfigureAwait(false);
        if (fromWarehouse is null)
            throw new NamEcommerceDomainException("Error.StockTransfer.FromWarehouseNotFound");

        if (fromWarehouse.WarehouseType == WarehouseType.DirectTransit)
            throw new NamEcommerceDomainException("Error.StockTransfer.CannotTransferFromDirectShipTransit");

        var toWarehouse = await warehouseDataReader.GetByIdAsync(dto.ToWarehouseId).ConfigureAwait(false);
        if (toWarehouse is null)
            throw new NamEcommerceDomainException("Error.StockTransfer.ToWarehouseNotFound");

        if (toWarehouse.WarehouseType == WarehouseType.DirectTransit)
            throw new NamEcommerceDomainException("Error.StockTransfer.CannotTransferToDirectShipTransit");

        var code = GenerateCode();
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var note = new StockTransferNote(code, dto.FromWarehouseId, fromWarehouse.Name,
            dto.ToWarehouseId, toWarehouse.Name, dto.Note, currentUser?.Id);

        foreach (var itemDto in dto.Items)
        {
            var product = await productDataReader.GetByIdAsync(itemDto.ProductId).ConfigureAwait(false);
            if (product is null)
                throw new NamEcommerceDomainException("Error.StockTransfer.ProductNotFound");

            note.AddItem(itemDto.ProductId, product.Name, itemDto.Quantity);
        }

        var inserted = await noteRepository.InsertAsync(note).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task ApproveAsync(Guid id)
    {
        var note = await noteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null) throw new StockTransferNoteNotFoundException(id);

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var userId = currentUser?.Id ?? Guid.Empty;

        foreach (var item in note.Items)
        {
            var costSummary = await inventoryCostingManager.GetCurrentCostSummaryAsync(item.ProductId).ConfigureAwait(false);
            var unitCost = costSummary.AverageCost;

            var fromStock = await stockManager.GetInventoryStockForProductAsync(item.ProductId, note.FromWarehouseId).ConfigureAwait(false);
            if (fromStock is null || fromStock.QuantityAvailable < item.Quantity)
                throw new StockTransferInsufficientStockException(item.ProductName, fromStock?.QuantityAvailable ?? 0, item.Quantity);

            await stockManager.TransferStockAsync(
                item.ProductId,
                note.FromWarehouseId,
                note.ToWarehouseId,
                item.Quantity,
                unitCost,
                note.Id,
                userId,
                $"Phiếu chuyển kho {note.Code}").ConfigureAwait(false);

            var occurredAtUtc = DateTime.UtcNow;
            var transferOutCost = await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = note.FromWarehouseId,
                Quantity = item.Quantity,
                MovementType = InventoryCostMovementType.TransferOut,
                ReferenceType = InventoryCostReferenceType.StockTransfer,
                ReferenceId = note.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = occurredAtUtc
            }).ConfigureAwait(false);

            await inventoryCostingManager.RegisterTransferInAsync(new RegisterInventoryTransferInCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = note.ToWarehouseId,
                Quantity = item.Quantity,
                UnitCost = transferOutCost.UnitCost,
                SourceStatus = transferOutCost.Status,
                ReferenceType = InventoryCostReferenceType.StockTransfer,
                ReferenceId = note.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = occurredAtUtc
            }).ConfigureAwait(false);

            item.UnitCost = transferOutCost.UnitCost;
        }

        note.Approve();
        await noteRepository.UpdateAsync(note).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var note = await noteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null) throw new StockTransferNoteNotFoundException(id);

        note.Cancel();
        await noteRepository.UpdateAsync(note).ConfigureAwait(false);
    }

    public async Task<StockTransferNoteDto?> GetByIdAsync(Guid id)
    {
        var note = await noteDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return note?.ToDto();
    }

    public Task<IPagedDataDto<StockTransferNoteDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? fromWarehouseId, StockTransferStatus? status)
    {
        var query = noteDataReader.DataSource;

        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(n => n.Code.Contains(keywords));
        if (fromWarehouseId.HasValue)
            query = query.Where(n => n.FromWarehouseId == fromWarehouseId.Value);
        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        query = query.OrderByDescending(n => n.CreatedOnUtc);

        var total = query.Count();
        if (total == 0)
            return Task.FromResult(PagedDataDto.Create(new List<StockTransferNoteDto>(), pageIndex, pageSize, 0));

        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedDataDto.Create(items.Select(n => n.ToDto()).ToList(), pageIndex, pageSize, total));
    }
}
