using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Services.Catalog;

namespace NamEcommerce.Api.GraphQl.Test.Helpers;

public static class CategoryAppService
{
    public static Mock<ICategoryAppService> GetAllCategoriesWillReturns(params CategoryDto[] categories)
    {
        var categoryAppServiceMock = new Mock<ICategoryAppService>();
        categoryAppServiceMock.Setup(categoryManager => categoryManager.GetAllCategoriesAsync()).ReturnsAsync(categories);

        return categoryAppServiceMock;
    }
}
