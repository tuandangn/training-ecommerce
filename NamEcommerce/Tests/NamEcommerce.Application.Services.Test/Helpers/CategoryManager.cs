using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class CategoryManager
{
    public static Mock<ICategoryManager> GetAllCategoriesWillReturns(params CategoryDto[] categories)
    {
        var categoryManagerMock = new Mock<ICategoryManager>();
        categoryManagerMock.Setup(categoryManager => categoryManager.GetAllCategoriesAsync()).ReturnsAsync(categories);

        return categoryManagerMock;
    }
}
