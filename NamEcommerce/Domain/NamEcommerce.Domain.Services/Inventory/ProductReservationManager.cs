using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Specifications.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class ProductReservationManager(
    IRepository<ProductReservationLedger> productReservationRepository,
    IEntityDataReader<ProductReservationLedger> productReservationDataReader) : IProductReservationManager
{
    public Task<decimal> GetTotalReservedAsync(Guid productId)
        => productReservationDataReader.DataSource
            .Where(entry => entry.ProductId == productId)
            .SumAsync(entry => entry.QuantityDelta);

    public Task<decimal> GetReservedForOrderAsync(Guid productId, Guid orderId)
    {
        return productReservationDataReader.ApplySpecification(new ProductReservationsOfOrderSpec(productId, orderId))
                .SumAsync(entry => entry.QuantityDelta);
    }

    public async Task<decimal> GetReleasedByReferenceAsync(Guid productId, Guid orderId, ProductReservationReason reason, Guid referenceId)
    {
        var releaseQty = await productReservationDataReader.DataSource
            .Where(entry => entry.ProductId == productId
                && entry.OrderId == orderId
                && entry.Reason == reason
                && entry.ReferenceId == referenceId
                && entry.QuantityDelta < 0)
            .SumAsync(entry => entry.QuantityDelta).ConfigureAwait(false);
        return Math.Abs(releaseQty);
    }

    public Task<decimal> GetReservedByReferenceAsync(Guid productId, Guid orderId, ProductReservationReason reason, Guid referenceId)
    {
        return productReservationDataReader.DataSource
                .Where(entry => entry.ProductId == productId
                    && entry.OrderId == orderId
                    && entry.Reason == reason
                    && entry.ReferenceId == referenceId
                    && entry.QuantityDelta > 0)
                .SumAsync(entry => entry.QuantityDelta);
    }

    public async Task<ProductReservationDto?> GetByProductIdAsync(Guid productId)
    {
        var entries = await productReservationDataReader.DataSource
            .Where(entry => entry.ProductId == productId)
            .ToListAsync().ConfigureAwait(false);

        if (entries.Count == 0)
            return null;

        return new ProductReservationDto
        {
            ProductId = productId,
            TotalReservedByOrder = entries.Sum(entry => entry.QuantityDelta),
            UpdatedOnUtc = entries.Max(entry => entry.CreatedOnUtc)
        };
    }

    public async Task ReserveAsync(Guid productId, decimal quantity, Guid orderId, ProductReservationReason reason, Guid? referenceId = null)
    {
        EnsureValid(productId, quantity, orderId);

        var entry = new ProductReservationLedger(productId, orderId, quantity, reason, referenceId);
        entry.MarkAsCreated();
        await productReservationRepository.InsertAsync(entry).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(Guid productId, decimal quantity,
        Guid orderId, ProductReservationReason reason, Guid? referenceId = null)
    {
        EnsureValid(productId, quantity, orderId);

        var reservedForOrder = await GetReservedForOrderAsync(productId, orderId).ConfigureAwait(false);
        if (reservedForOrder < quantity)
            throw new InvalidStockOperationException("Error.CannotReleaseMoreThanReserved", reservedForOrder, quantity);

        var entry = new ProductReservationLedger(productId, orderId, -quantity, reason, referenceId);
        entry.MarkAsCreated();
        await productReservationRepository.InsertAsync(entry).ConfigureAwait(false);
    }

    public Task AdjustAsync(Guid productId, decimal deltaQuantity, Guid orderId,
        ProductReservationReason reserveReason, ProductReservationReason releaseReason, Guid? referenceId = null)
    {
        if (deltaQuantity == 0)
            return Task.CompletedTask;

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
