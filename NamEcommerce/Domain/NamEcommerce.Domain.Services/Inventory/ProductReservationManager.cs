using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class ProductReservationManager : IProductReservationManager
{
    private readonly IRepository<ProductReservationLedger> _repository;
    private readonly IEntityDataReader<ProductReservationLedger> _dataReader;

    public ProductReservationManager(
        IRepository<ProductReservationLedger> repository,
        IEntityDataReader<ProductReservationLedger> dataReader)
    {
        _repository = repository;
        _dataReader = dataReader;
    }

    public Task<decimal> GetTotalReservedAsync(Guid productId)
        => Task.FromResult(_dataReader.DataSource
            .Where(entry => entry.ProductId == productId)
            .Sum(entry => entry.QuantityDelta));

    public Task<decimal> GetReservedForOrderAsync(Guid productId, Guid orderId)
        => Task.FromResult(_dataReader.DataSource
            .Where(entry => entry.ProductId == productId && entry.OrderId == orderId)
            .Sum(entry => entry.QuantityDelta));

    public Task<ProductReservationDto?> GetByProductIdAsync(Guid productId)
    {
        var entries = _dataReader.DataSource
            .Where(entry => entry.ProductId == productId)
            .ToList();

        if (entries.Count == 0)
            return Task.FromResult<ProductReservationDto?>(null);

        return Task.FromResult<ProductReservationDto?>(new ProductReservationDto
        {
            ProductId = productId,
            TotalReservedByOrder = entries.Sum(entry => entry.QuantityDelta),
            UpdatedOnUtc = entries.Max(entry => entry.CreatedOnUtc)
        });
    }

    public async Task ReserveAsync(
        Guid productId,
        decimal quantity,
        Guid orderId,
        ProductReservationReason reason,
        Guid? referenceId = null)
    {
        EnsureValid(productId, quantity, orderId);

        var entry = new ProductReservationLedger(productId, orderId, quantity, reason, referenceId);
        entry.MarkAsCreated();
        await _repository.InsertAsync(entry).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(
        Guid productId,
        decimal quantity,
        Guid orderId,
        ProductReservationReason reason,
        Guid? referenceId = null)
    {
        EnsureValid(productId, quantity, orderId);

        var reservedForOrder = await GetReservedForOrderAsync(productId, orderId).ConfigureAwait(false);
        if (reservedForOrder < quantity)
            throw new InvalidStockOperationException(
                "Error.CannotReleaseMoreThanReserved",
                reservedForOrder,
                quantity);

        var entry = new ProductReservationLedger(productId, orderId, -quantity, reason, referenceId);
        entry.MarkAsCreated();
        await _repository.InsertAsync(entry).ConfigureAwait(false);
    }

    public Task AdjustAsync(
        Guid productId,
        decimal deltaQuantity,
        Guid orderId,
        ProductReservationReason reserveReason,
        ProductReservationReason releaseReason,
        Guid? referenceId = null)
    {
        if (deltaQuantity == 0) return Task.CompletedTask;
        return deltaQuantity > 0
            ? ReserveAsync(productId, deltaQuantity, orderId, reserveReason, referenceId)
            : ReleaseAsync(productId, -deltaQuantity, orderId, releaseReason, referenceId);
    }

    private static void EnsureValid(Guid productId, decimal quantity, Guid orderId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        if (orderId == Guid.Empty)
            throw new InvalidStockOperationException("Error.OrderRequired");

        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");
    }
}
