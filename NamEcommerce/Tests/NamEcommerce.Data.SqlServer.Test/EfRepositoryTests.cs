using NamEcommerce.Data.SqlServer.Test.Helpers;

namespace NamEcommerce.Data.SqlServer.Test;

public sealed class EfRepositoryTests
{
    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new EfRepository<object>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_EntityIsNotNull_DeleteEntity()
    {
        var entity = new object();
        var dbContextMock = EfDbContext.Create()
            .WhenCall(dbContext => dbContext.Remove(entity))
            .WhenCall(dbContext => dbContext.SaveChangesAsync(default), Task.FromResult(1));
        var repository = new EfRepository<object>(dbContextMock.Object);

        await repository.DeleteAsync(entity);

        dbContextMock.Verify();
    }

    #endregion

    #region InsertAsync

    [Fact]
    public async Task InsertAsync_EntityIsNull_ThrowsArgumentNullException()
    {
        var repository = new EfRepository<object>(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.InsertAsync(null!));
    }

    [Fact]
    public async Task InsertAsync_EntityIsNotNull_InsertEntity()
    {
        var entity = new object();
        var returnEntity = new object();
        var dbContextMock = EfDbContext.Create()
            .WhenCall(dbContext => dbContext.AddAsync(entity, default), ValueTask.FromResult(returnEntity))
            .WhenCall(dbContext => dbContext.SaveChangesAsync(default), Task.FromResult(1));
        var repository = new EfRepository<object>(dbContextMock.Object);

        var insertedEntity = await repository.InsertAsync(entity);

        Assert.Equal(insertedEntity, returnEntity);
        dbContextMock.Verify();
    }

    #endregion
}
