using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Api.GraphQl.Models.Catalog;
using NamEcommerce.Api.GraphQl.Schemes.Catalog.Types;
using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog;

public sealed class CatalogQuery : ObjectGraphType
{
    public CatalogQuery(IDataLoaderContextAccessor loaderAccessor)
    {
        Name = "CatalogQuery";
        Description = "Describes catalog queries";

        Field<ListGraphType<NonNullGraphType<CategoryType>>>("allCategories")
            .Description("Get all categories")
            .ResolveAsync(async context =>
            {
                var categoryDataLoader = context.RequestServices!.GetRequiredService<ICategoryDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddLoader(CategoryDataLoader.GET_ALL, categoryDataLoader.GetAllCategoriesAsync);
                var allCategories = await loader.LoadAsync().GetResultAsync(context.CancellationToken);
                return allCategories.Select(category => new CategoryModel(category.Id, category.Name)
                {
                    ParentId = category.ParentId
                }).ToList();
            });
        Field<CategoryType>("category")
            .Description("Get category by id")
            .Argument<NonNullGraphType<IdGraphType>>("id", "Category id")
            .ResolveAsync(async context =>
            {
                var categoryDataLoader = context.RequestServices!.GetRequiredService<ICategoryDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddBatchLoader<Guid, CategoryDto>(CategoryDataLoader.GET_BY_ID, categoryDataLoader.GetCategoriesByIdsAsync);
                var foundCategory = await loader.LoadAsync(context.GetArgument<Guid>("id")).GetResultAsync(context.CancellationToken);
                if (foundCategory == null)
                    return null;
                return new CategoryModel(foundCategory.Id, foundCategory.Name)
                {
                    ParentId = foundCategory.ParentId
                };
            });
    }
}
