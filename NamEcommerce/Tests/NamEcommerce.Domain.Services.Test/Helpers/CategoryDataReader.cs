using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class CategoryDataReader
{
    public static Mock<IEntityDataReader<Category>> HasOne(Category category)
        => EntityDataReader.Create<Category>().WithData(category);

    public static Mock<IEntityDataReader<Category>> SetData(params Category[] categories)
        => EntityDataReader.Create<Category>().WithData(categories);

    public static Mock<IEntityDataReader<Category>> Empty()
        => EntityDataReader.Create<Category>().WithData(Array.Empty<Category>());
}
