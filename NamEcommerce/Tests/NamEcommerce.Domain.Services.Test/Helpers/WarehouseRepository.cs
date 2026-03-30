using NamEcommerce.Domain.Entities.Inventory;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class WarehouseRepository
{
    public static Mock<IRepository<Warehouse>> CreateWarehouseWillReturns(Warehouse @return)
        => Repository.Create<Warehouse>().WhenCall(r => 
            r.InsertAsync(It.Is<Warehouse>(entity => 
                entity.Code == @return.Code && entity.Name == @return.Name 
                && entity.PhoneNumber == @return.PhoneNumber && entity.Address == @return.Address
                && entity.IsActive == @return.IsActive && entity.WarehouseType == @return.WarehouseType
                && entity.ManagerUserId == @return.ManagerUserId
            ))
        , @return);

    public static Mock<IRepository<Warehouse>> UpdateWarehouseWillReturns(Warehouse @return)
        => Repository.Create<Warehouse>().WhenCall(r =>
            r.UpdateAsync(It.Is<Warehouse>(entity =>
                entity.Code == @return.Code && entity.Name == @return.Name
                && entity.PhoneNumber == @return.PhoneNumber && entity.Address == @return.Address
                && entity.IsActive == @return.IsActive && entity.WarehouseType == @return.WarehouseType
                && entity.ManagerUserId == @return.ManagerUserId
            ))
        , @return);

    public static Mock<IRepository<Warehouse>> NotFound(Guid id)
        => Repository.Create<Warehouse>().WhenCall(r => r.GetByIdAsync(id, default), (Warehouse?)null);
    public static Mock<IRepository<Warehouse>> NotFound(this Mock<IRepository<Warehouse>> mock, Guid id)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), (Warehouse?)null);

    public static Mock<IRepository<Warehouse>> WarehouseById(Guid id, Warehouse @return)
        => Repository.Create<Warehouse>().WhenCall(r => r.GetByIdAsync(id, default), @return);
    public static Mock<IRepository<Warehouse>> WarehouseById(this Mock<IRepository<Warehouse>> mock, Guid id, Warehouse @return)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<Warehouse>> CanDeleteWarehouse(Warehouse warehouse)
        => Repository.Create<Warehouse>().CanCall(r => r.DeleteAsync(warehouse));
}
