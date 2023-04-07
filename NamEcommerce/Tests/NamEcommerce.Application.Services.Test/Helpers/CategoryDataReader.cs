using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.TestHelper;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class CategoryDataReader
{
    public static Mock<IEntityDataReader<Category>> SetData(params Category[] categories) 
        => EntityDataReader.Create<Category>()
            .WithData(categories);

    public static Mock<IEntityDataReader<Category>> NotFound(Guid id) 
        => EntityDataReader.Create<Category>()
            .WhenCall(reader => reader.GetByIdAsync(id, default), (Category)null!);

    public static Mock<IEntityDataReader<Category>> CategoryById(Guid id, Category category)
        => EntityDataReader.Create<Category>()
            .WhenCall(reader => reader.GetByIdAsync(id, default), category);

    public static Mock<IEntityDataReader<Category>> CategoriesByIds(Guid[] ids, params Category[] categories) 
        => EntityDataReader.Create<Category>()
            .WhenCall(reader => reader.GetByIdsAsync(ids), categories);

    public static Mock<IEntityDataReader<Category>> AllCategories(params Category[] categories)
        => EntityDataReader.Create<Category>()
            .WhenCall(reader => reader.GetAllAsync(), categories);
}
