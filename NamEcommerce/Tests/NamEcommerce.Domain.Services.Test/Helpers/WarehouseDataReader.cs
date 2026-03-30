using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class WarehouseDataReader
{
    public static Mock<IEntityDataReader<Warehouse>> Empty()
        => EntityDataReader.Create<Warehouse>().WithData(Array.Empty<Warehouse>());

    public static Mock<IEntityDataReader<Warehouse>> WithData(params Warehouse[] warehouses)
        => EntityDataReader.Create<Warehouse>().WithData(warehouses);
    public static Mock<IEntityDataReader<Warehouse>> HasOne(Warehouse warehouse)
        => EntityDataReader.Create<Warehouse>().WithData(warehouse);

    public static Mock<IEntityDataReader<Warehouse>> NotFound(Guid id)
        => EntityDataReader.Create<Warehouse>().WhenCall(reader => reader.GetByIdAsync(id), (Warehouse?)null);

    public static Mock<IEntityDataReader<Warehouse>> WarehouseById(Guid id, Warehouse warehouse)
        => EntityDataReader.Create<Warehouse>().WhenCall(reader => reader.GetByIdAsync(id), warehouse);
    public static Mock<IEntityDataReader<Warehouse>> WarehouseById(this Mock<IEntityDataReader<Warehouse>> mock, Guid id, Warehouse warehouse)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), warehouse);
}
