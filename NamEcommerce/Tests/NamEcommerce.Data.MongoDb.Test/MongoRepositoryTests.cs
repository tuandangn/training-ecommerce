using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.TestHelper;

namespace NamEcommerce.Data.MongoDb.Test;

public sealed class MongoRepositoryTests
{
    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new NamECommerceMongoRepository<Category>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteAsync(null!, default));
    }

    [Fact]
    public async Task DeleteAsync_EntityIsNotNull_DeleteEntity()
    {
        var entity = new Category(default, string.Empty);
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.RemoveAsync(entity, default));
        var repository = new NamECommerceMongoRepository<Category>(dbContextMock.Object);

        await repository.DeleteAsync(entity, default);

        dbContextMock.Verify();
    }

    #endregion

    #region InsertAsync

    [Fact]
    public async Task InsertAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new NamECommerceMongoRepository<Category>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.InsertAsync(null!, default));
    }

    [Fact]
    public async Task InsertAsync_EntityIsNotNull_InsertEntity()
    {
        var entity = new Category(default, string.Empty);
        var returnEntity = new Category(Guid.NewGuid(), string.Empty);
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.AddAsync(entity, default), Task.FromResult(returnEntity));
        var repository = new NamECommerceMongoRepository<Category>(dbContextMock.Object);

        var insertedEntity = await repository.InsertAsync(entity, default);

        Assert.Equal(insertedEntity, returnEntity);
        dbContextMock.Verify();
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllData()
    {
        var returnValues = new[] { new Category(default, string.Empty), new Category(default, string.Empty), new Category(default, string.Empty) };
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.GetDataAsync<Category>(), returnValues.AsQueryable());
        var repository = new NamECommerceMongoRepository<Category>(dbContextMock.Object);

        var allData = await repository.GetAllAsync();

        Assert.Equal(3, allData.Count());
        Assert.Equal(returnValues[0], allData.ElementAt(0));
        Assert.Equal(returnValues[1], allData.ElementAt(1));
        Assert.Equal(returnValues[2], allData.ElementAt(2));
        dbContextMock.Verify();
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var dbContextMock = DbContext.Create().WhenCall(dbContext => dbContext.FindAsync<Category>(id, default), (Category)null!);
        var repository = new NamECommerceMongoRepository<Category>(dbContextMock.Object);

        var notFound = await repository.GetByIdAsync(id, default);

        Assert.Null(notFound);
        dbContextMock.Verify();
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsFoundEntity()
    {
        var id = Guid.NewGuid();
        var entity = new Category(default, string.Empty);
        var dbContextMock = DbContext.Create().WhenCall(dbContext => dbContext.FindAsync<Category>(id, default), entity);
        var repository = new NamECommerceMongoRepository<Category>(dbContextMock.Object);

        var found = await repository.GetByIdAsync(id, default);

        Assert.Equal(entity, found);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new NamECommerceMongoRepository<Category>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!, default));
    }

    [Fact]
    public async Task UpdateAsync_EntityIsNotNull_UpdateEntity()
    {
        var entity = new Category(default, string.Empty);
        var returnEntity = new Category(Guid.NewGuid(), string.Empty);
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.UpdateAsync(entity, default), returnEntity);
        var repository = new NamECommerceMongoRepository<Category>(dbContextMock.Object);

        var updatedEntity = await repository.UpdateAsync(entity, default);

        Assert.Equal(returnEntity, updatedEntity);
        dbContextMock.Verify();
    }

    #endregion
}
