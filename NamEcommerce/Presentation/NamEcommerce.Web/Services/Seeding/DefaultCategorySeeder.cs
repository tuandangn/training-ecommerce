using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class DefaultCategorySeeder(ICategoryAppService categoryAppService) : IDataSeeder
{
    private static readonly string[] DefaultCategories =
    [
        "Xi măng", "Cát / Đá", "Bàn cầu", "Lavabo", "Phụ kiện", "Sắt / Thép", "Gạch men",
        "Gạch xây dựng", "Vòi nước"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await categoryAppService
            .GetCategoriesAsync(pageSize: 200)
            .ConfigureAwait(false);

        if (existing.Pagination.TotalCount > 0)
            return;

        foreach (var categoryName in DefaultCategories)
        {
            await categoryAppService.CreateCategoryAsync(new CreateCategoryAppDto
            {
                Name = categoryName,
                DisplayOrder = 1,
                ParentId = null
            }).ConfigureAwait(false);
        }
    }
}
