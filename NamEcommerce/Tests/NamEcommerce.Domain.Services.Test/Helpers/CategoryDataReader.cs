using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class CategoryDataReader
{
    public static Mock<IEntityDataReader<Category>> HasOne(Category category)
        => EntityDataReader.Create<Category>().WithData(category);

    public static Mock<IEntityDataReader<Category>> CategoryById(Guid id, Category category)
        => EntityDataReader.Create<Category>().WhenCall(reader => reader.GetByIdAsync(id), category);
    public static Mock<IEntityDataReader<Category>> CategoryById(this Mock<IEntityDataReader<Category>> mock, Guid id, Category category)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), category);

    public static Mock<IEntityDataReader<Category>> NotFound(Guid id)
        => EntityDataReader.Create<Category>().WhenCall(reader => reader.GetByIdAsync(id), (Category?) null);

    public static Mock<IEntityDataReader<Category>> SetData(params Category[] categories)
        => EntityDataReader.Create<Category>().WithData(categories);

    public static Mock<IEntityDataReader<Category>> AllCategories(this Mock<IEntityDataReader<Category>> mock, params Category[] categories)
        => mock.WhenCall(m => m.GetAllAsync(), categories);

    public static Mock<IEntityDataReader<Category>> Empty()
        => EntityDataReader.Create<Category>().WithData(Array.Empty<Category>());
}
