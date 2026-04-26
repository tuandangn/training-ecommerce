using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Services.Inventory;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class InventoryStockManagerTests
{
    #region UpdateAverageCostAsync

    [Fact]
    public async Task UpdateAverageCostAsync_NewAverageCostIsNegative_ThrowsInvalidStockOperationException()
    {
        // Arrange — không cần mock data reader vì validation chạy trước khi truy cập store
        var manager = new InventoryStockManager(null!, null!, null!, null!, null!, null!, null!);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidStockOperationException>(() =>
            manager.UpdateAverageCostAsync(Guid.NewGuid(), Guid.NewGuid(), -1m));

        Assert.Equal("Error.StockAverageCostCannotBeNegative", ex.ErrorCode);
    }

    [Fact]
    public async Task UpdateAverageCostAsync_StockNotFound_ThrowsStockNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var stockDataReaderStub = InventoryStockDataReader.Empty();
        var manager = new InventoryStockManager(
            inventoryStockRepository: null!,
            inventoryStockDataReader: stockDataReaderStub.Object,
            stockAuditLogger: null!,
            stockMovementRepository: null!,
            productDataReader: null!,
            warehouseDataReader: null!,
            stockMovementDataReader: null!);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<StockNotFoundException>(() =>
            manager.UpdateAverageCostAsync(productId, warehouseId, 100m));

        Assert.Equal("Error.StockNotFound", ex.ErrorCode);
        stockDataReaderStub.Verify();
    }

    [Fact]
    public async Task UpdateAverageCostAsync_AverageCostUnchanged_DoesNotCallUpdate()
    {
        // Arrange — stock đã có AverageCost = 50, gọi update với cùng giá trị → idempotent skip
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existing = new InventoryStock(Guid.NewGuid(), productId, warehouseId, Guid.NewGuid())
        {
            AverageCost = 50m
        };
        var stockDataReaderStub = InventoryStockDataReader.HasOne(existing);
        var stockRepoMock = InventoryStockRepository.Create();
        var manager = new InventoryStockManager(
            inventoryStockRepository: stockRepoMock.Object,
            inventoryStockDataReader: stockDataReaderStub.Object,
            stockAuditLogger: null!,
            stockMovementRepository: null!,
            productDataReader: null!,
            warehouseDataReader: null!,
            stockMovementDataReader: null!);

        // Act
        await manager.UpdateAverageCostAsync(productId, warehouseId, 50m);

        // Assert
        stockRepoMock.Verify(r => r.UpdateAsync(It.IsAny<InventoryStock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAverageCostAsync_DataIsValid_UpdatesAverageCostAndCallsRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var newAverageCost = 125.50m;
        var existing = new InventoryStock(Guid.NewGuid(), productId, warehouseId, Guid.NewGuid())
        {
            AverageCost = 100m
        };
        var stockDataReaderStub = InventoryStockDataReader.HasOne(existing);
        var stockRepoMock = InventoryStockRepository.ExpectsUpdateAverageCost(newAverageCost);
        var manager = new InventoryStockManager(
            inventoryStockRepository: stockRepoMock.Object,
            inventoryStockDataReader: stockDataReaderStub.Object,
            stockAuditLogger: null!,
            stockMovementRepository: null!,
            productDataReader: null!,
            warehouseDataReader: null!,
            stockMovementDataReader: null!);

        var beforeUpdatedOnUtc = existing.UpdatedOnUtc;

        // Act
        await manager.UpdateAverageCostAsync(productId, warehouseId, newAverageCost);

        // Assert
        Assert.Equal(newAverageCost, existing.AverageCost);
        Assert.True(existing.UpdatedOnUtc >= beforeUpdatedOnUtc);
        stockRepoMock.Verify();
        stockDataReaderStub.Verify();
    }

    [Fact]
    public async Task UpdateAverageCostAsync_NewAverageCostIsZero_AllowedAndPersisted()
    {
        // Arrange — giá vốn = 0 hợp lệ (ví dụ trường hợp tất cả item đã được chỉnh về 0 hoặc xóa)
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existing = new InventoryStock(Guid.NewGuid(), productId, warehouseId, Guid.NewGuid())
        {
            AverageCost = 100m
        };
        var stockDataReaderStub = InventoryStockDataReader.HasOne(existing);
        var stockRepoMock = InventoryStockRepository.ExpectsUpdateAverageCost(0m);
        var manager = new InventoryStockManager(
            inventoryStockRepository: stockRepoMock.Object,
            inventoryStockDataReader: stockDataReaderStub.Object,
            stockAuditLogger: null!,
            stockMovementRepository: null!,
            productDataReader: null!,
            warehouseDataReader: null!,
            stockMovementDataReader: null!);

        // Act
        await manager.UpdateAverageCostAsync(productId, warehouseId, 0m);

        // Assert
        Assert.Equal(0m, existing.AverageCost);
        stockRepoMock.Verify();
    }

    #endregion
}
