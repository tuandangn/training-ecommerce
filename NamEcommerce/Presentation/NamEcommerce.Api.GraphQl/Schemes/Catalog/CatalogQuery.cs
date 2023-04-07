using GraphQL.DataLoader;
using GraphQL.Types;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Api.GraphQl.Models.Catalog;
using NamEcommerce.Api.GraphQl.Schemes.Catalog.Types;

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
    }
}
