using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class CategoryManager
{
    public static Mock<ICategoryManager> WhenGetCategoriesReturns(string keywords, int pageIndex, int pageSize, IPagedDataDto<CategoryDto> @return)
    {
        var mock = new Mock<ICategoryManager>();
        mock.Setup(r => r.GetCategoriesAsync(keywords, pageIndex, pageSize)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<ICategoryManager> SetUsernameExists(string name, bool exists)
    {
        var mock = new Mock<ICategoryManager>();
        mock.Setup(r => r.DoesNameExistAsync(name, null)).ReturnsAsync(exists).Verifiable();

        return mock;
    }

    public static Mock<ICategoryManager> SetUsernameExists(string name, Guid compareId, bool exists)
    {
        var mock = new Mock<ICategoryManager>();
        mock.Setup(r => r.DoesNameExistAsync(name, compareId)).ReturnsAsync(exists).Verifiable();

        return mock;
    }

    public static Mock<ICategoryManager> CreateCategoryReturns(this Mock<ICategoryManager> mock, CreateCategoryDto dto, CreateCategoryResultDto @return)
    {
        mock.Setup(r => r.CreateCategoryAsync(dto)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<ICategoryManager> UpdateCategoryReturns(this Mock<ICategoryManager> mock, UpdateCategoryDto dto, UpdateCategoryResultDto @return)
    {
        mock.Setup(r => r.UpdateCategoryAsync(dto)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<ICategoryManager> CanDeleteCategory(Guid id)
    {
        var mock = new Mock<ICategoryManager>();

        mock.Setup(r => r.DeleteCategoryAsync(id)).Returns(Task.CompletedTask).Verifiable();

        return mock;
    }
}
