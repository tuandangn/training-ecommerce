using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Test.Helpers;

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

        var entityDataReader = new EntityDataReader<Category>(dbContextMock.Object, null!);
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
        var repositoryMock = CategoryRepository.NotFound(id);
        var entityDataReader = new EntityDataReader<Category>(null!, repositoryMock.Object);

        var notFound = await entityDataReader.GetByIdAsync(id);

        Assert.Null(notFound);
        repositoryMock.Verify();
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsFoundEntity()
    {
        var id = Guid.NewGuid();
        var entity = new Category(default, string.Empty);
        var repositoryMock = CategoryRepository.CategoryById(id, entity);
        var entityDataReader = new EntityDataReader<Category>(null!, repositoryMock.Object);

        var found = await entityDataReader.GetByIdAsync(id, default);

        Assert.Equal(entity, found);
        repositoryMock.Verify();
    }

    #endregion

    #region GetByIdsAsync

    [Fact]
    public async Task GetByIdsAsync_IdsIsEmpty_ReturnEmpty()
    {
        var entityDataReader = new EntityDataReader<Category>(null!, null!);

        var emptyResult = await entityDataReader.GetByIdsAsync(Enumerable.Empty<Guid>());

        Assert.Empty(emptyResult);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var categories = new[]
        {
            new Category(ids[0], "Category 1"),
            new Category(ids[1], "Category 2")
        };
        var dbContextMock = DbContext.Create()
            .WhenCall(dbContext => dbContext.GetDataSource<Category>(), categories);
        var entityDataReader = new EntityDataReader<Category>(dbContextMock.Object, null!);

        var result = await entityDataReader.GetByIdsAsync(ids);

        Assert.Equal(categories, result);
        dbContextMock.Verify();
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnUnorderedData()
    {
        var data = new[]{
            new Category(Guid.NewGuid(), "category 2") { DisplayOrder = 2 },
            new Category(Guid.NewGuid(), "category 1") { DisplayOrder = 1 }
        };
        var repositoryMock = CategoryRepository.SetData(data);
        var entityDataReader = new EntityDataReader<Category>(null!, repositoryMock.Object);
        var unorderedCategories = await entityDataReader.GetAllAsync();

        Assert.Collection(unorderedCategories,
            cat1 => Assert.Equal(data[0].Id, cat1.Id),
            cat2 => Assert.Equal(data[1].Id, cat2.Id));
        repositoryMock.Verify();
    }

    #endregion
}
