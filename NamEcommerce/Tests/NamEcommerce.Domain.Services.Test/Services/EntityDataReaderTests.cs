using NamEcommerce.Domain.Services.Common;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class EntityDataReaderTests
{
    #region DataSource

    [Fact]
    public Task DataSource_ReturnsUnorderedData()
    {
        var data = new[]{
            new Category(Guid.NewGuid(), "category 2") { DisplayOrder = 2 },
            new Category(Guid.NewGuid(), "category 1") { DisplayOrder = 1 }
        };
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.GetDataSource<Category>(), data);

        var entityDataReader = new EntityDataReader<Category>(dbContextMock.Object);
        var unorderedCategories = entityDataReader.DataSource;

        Assert.Collection(unorderedCategories,
            cat1 => Assert.Equal(data[0].Id, cat1.Id),
            cat2 => Assert.Equal(data[1].Id, cat2.Id));
        dbContextMock.Verify();

        return Task.CompletedTask;
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var dbContextMock = DbContext.Create().WhenCall(dbContext => dbContext.FindAsync<Category>(id, default), (Category)null!);
        var entityDataReader = new EntityDataReader<Category>(dbContextMock.Object);

        var notFound = await entityDataReader.GetByIdAsync(id);

        Assert.Null(notFound);
        dbContextMock.Verify();
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsFoundEntity()
    {
        var id = Guid.NewGuid();
        var entity = new Category(default, string.Empty);
        var dbContextMock = DbContext.Create().WhenCall(dbContext => dbContext.FindAsync<Category>(id, default), entity);
        var entityDataReader = new EntityDataReader<Category>(dbContextMock.Object);

        var found = await entityDataReader.GetByIdAsync(id, default);

        Assert.Equal(entity, found);
    }

    #endregion
}
