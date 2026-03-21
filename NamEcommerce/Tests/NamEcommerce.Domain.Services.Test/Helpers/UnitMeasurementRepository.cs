namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class UnitMeasurementRepository
{
    public static Mock<IRepository<UnitMeasurement>> CreateUnitMeasurementWillReturns(UnitMeasurement @return)
        //*TODO* check inserting data
        => Repository.Create<UnitMeasurement>().WhenCall(r => 
            r.InsertAsync(It.Is<UnitMeasurement>(entity => entity.Name == @return.Name && entity.DisplayOrder == entity.DisplayOrder))
        , @return);

    public static Mock<IRepository<UnitMeasurement>> NotFound(Guid id)
        => Repository.Create<UnitMeasurement>().WhenCall(r => r.GetByIdAsync(id, default), (UnitMeasurement?)null);
    public static Mock<IRepository<UnitMeasurement>> NotFound(this Mock<IRepository<UnitMeasurement>> mock, Guid id)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), (UnitMeasurement?)null);

    public static Mock<IRepository<UnitMeasurement>> UnitMeasurementById(Guid id, UnitMeasurement @return)
        => Repository.Create<UnitMeasurement>().WhenCall(r => r.GetByIdAsync(id, default), @return);
    public static Mock<IRepository<UnitMeasurement>> UnitMeasurementById(this Mock<IRepository<UnitMeasurement>> mock, Guid id, UnitMeasurement @return)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<UnitMeasurement>> CanDeleteUnitMeasurement(UnitMeasurement unitMeasurement)
        => Repository.Create<UnitMeasurement>().CanCall(r => r.DeleteAsync(unitMeasurement));
}
