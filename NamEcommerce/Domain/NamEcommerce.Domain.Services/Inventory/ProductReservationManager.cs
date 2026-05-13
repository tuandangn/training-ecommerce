using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class ProductReservationManager : IProductReservationManager
{
    private readonly IRepository<ProductReservation> _repository;
    private readonly IEntityDataReader<ProductReservation> _dataReader;

    public ProductReservationManager(
        IRepository<ProductReservation> repository,
        IEntityDataReader<ProductReservation> dataReader)
    {
        _repository = repository;
        _dataReader = dataReader;
    }

    public async Task<decimal> GetTotalReservedAsync(Guid productId)
    {
        var entry = await GetEntryAsync(productId).ConfigureAwait(false);
        return entry?.TotalReservedByOrder ?? 0m;
    }

    public async Task<ProductReservationDto?> GetByProductIdAsync(Guid productId)
    {
        var entry = await GetEntryAsync(productId).ConfigureAwait(false);
        return entry?.ToDto();
    }

    public async Task ReserveAsync(Guid productId, decimal quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var entry = await GetEntryAsync(productId).ConfigureAwait(false);
        if (entry is null)
        {
            var created = new ProductReservation(productId);
            created.TotalReservedByOrder = quantity;
            created.UpdatedOnUtc = DateTime.UtcNow;
            await _repository.InsertAsync(created).ConfigureAwait(false);
            return;
        }

        entry.TotalReservedByOrder += quantity;
        entry.UpdatedOnUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(entry).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(Guid productId, decimal quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var entry = await GetEntryAsync(productId).ConfigureAwait(false);
        if (entry is null || entry.TotalReservedByOrder < quantity)
            throw new InvalidStockOperationException(
                "Error.CannotReleaseMoreThanReserved",
                entry?.TotalReservedByOrder ?? 0m,
                quantity);

        entry.TotalReservedByOrder -= quantity;
        entry.UpdatedOnUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(entry).ConfigureAwait(false);
    }

    public Task AdjustAsync(Guid productId, decimal deltaQuantity, Guid orderId)
    {
        if (deltaQuantity == 0) return Task.CompletedTask;
        return deltaQuantity > 0
            ? ReserveAsync(productId, deltaQuantity, orderId)
            : ReleaseAsync(productId, -deltaQuantity, orderId);
    }

    private Task<ProductReservation?> GetEntryAsync(Guid productId)
    {
        return Task.Run(() => _dataReader.DataSource
            .SingleOrDefault(r => r.ProductId == productId));
    }
}
