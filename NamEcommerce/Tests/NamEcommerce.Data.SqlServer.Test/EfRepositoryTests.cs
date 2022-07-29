using NamEcommerce.Data.SqlServer.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Data.SqlServer.Test;

public sealed class EfRepositoryTests
{
    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new EfRepository<Category>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_EntityIsNotNull_DeleteEntity()
    {
        var entity = new Category(default, string.Empty);
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.Remove(entity))
            .WhenCall(dbContext => dbContext.SaveChangesAsync(default), Task.FromResult(1));
        var repository = new EfRepository<Category>(dbContextMock.Object);

        await repository.DeleteAsync(entity);

        dbContextMock.Verify();
    }

    #endregion

    #region InsertAsync

    [Fact]
    public async Task InsertAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new EfRepository<Category>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.InsertAsync(null!));
    }

    [Fact]
    public async Task InsertAsync_EntityIsNotNull_InsertEntity()
    {
        var entity = new Category(default, string.Empty);
        var returnEntity = new Category(1, string.Empty);
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.AddAsync(entity, default), ValueTask.FromResult(returnEntity))
            .WhenCall(dbContext => dbContext.SaveChangesAsync(default), Task.FromResult(1));
        var repository = new EfRepository<Category>(dbContextMock.Object);

        var insertedEntity = await repository.InsertAsync(entity);

        Assert.Equal(insertedEntity, returnEntity);
        dbContextMock.Verify();
    }

    #endregion

    #region GetAllAsync

    public async Task GetAllAsync_ReturnsAllData()
    {
        var returnValues = new[] { new Category(default, string.Empty), new Category(default, string.Empty), new Category(default, string.Empty) };
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.GetData<Category>(), returnValues.AsQueryable());
        var repository = new EfRepository<Category>(dbContextMock.Object);

        var allData = await repository.GetAllAsync();

        Assert.Equal(3, allData.Count());
        Assert.Equal(returnValues[0], allData.ElementAt(0));
        Assert.Equal(returnValues[1], allData.ElementAt(1));
        Assert.Equal(returnValues[2], allData.ElementAt(2));
        dbContextMock.Verify();
    }

    #endregion

    #region GetByIdAsync

    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var id = 0;
        var dbContextMock = DbContext.Create().WhenCall(dbContext => dbContext.FindAsync<Category>(id, default), null);
        var repository = new EfRepository<Category>(dbContextMock.Object);

        var notFound = await repository.GetByIdAsync(id);

        Assert.Null(notFound);
        dbContextMock.Verify();
    }

    public async Task GetByIdAsync_Found_ReturnsFoundEntity()
    {
        var id = 1;
        var entity = new Category(default, string.Empty);
        var dbContextMock = DbContext.Create().WhenCall(dbContext => dbContext.FindAsync<Category>(id, default), entity);
        var repository = new EfRepository<Category>(dbContextMock.Object);

        var found = await repository.GetByIdAsync(id);

        Assert.Equal(entity, found);
    }

    #endregion

    #region UpdateAsync

    public async Task UpdateAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new EfRepository<Category>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
    }

    public async Task UpdateAsync_EntityIsNotNull_UpdateEntity()
    {
        var entity = new Category(default, string.Empty);
        var returnEntity = new Category(1, string.Empty);
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.Update(entity), returnEntity)
            .WhenCall(dbContext => dbContext.SaveChangesAsync(default), 1);
        var repository = new EfRepository<Category>(dbContextMock.Object);

        var updatedEntity = await repository.UpdateAsync(entity);

        Assert.Equal(returnEntity, updatedEntity);
        dbContextMock.Verify();
    }

    #endregion
}
