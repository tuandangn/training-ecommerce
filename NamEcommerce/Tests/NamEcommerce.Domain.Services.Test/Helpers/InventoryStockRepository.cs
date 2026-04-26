using NamEcommerce.Domain.Entities.Inventory;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class InventoryStockRepository
{
    public static Mock<IRepository<InventoryStock>> Create()
        => Repository.Create<InventoryStock>();

    /// <summary>
    /// Setup mock cho phép verify rằng UpdateAsync ĐƯỢC GỌI với stock có AverageCost == expectedAverageCost.
    /// </summary>
    public static Mock<IRepository<InventoryStock>> ExpectsUpdateAverageCost(decimal expectedAverageCost)
    {
        var mock = Create();
        mock.Setup(r => r.UpdateAsync(
                It.Is<InventoryStock>(s => s.AverageCost == expectedAverageCost),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryStock s, CancellationToken _) => s)
            .Verifiable();
        return mock;
    }
}
