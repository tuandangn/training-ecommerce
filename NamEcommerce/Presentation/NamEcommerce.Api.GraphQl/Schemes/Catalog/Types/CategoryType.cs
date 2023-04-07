using GraphQL.DataLoader;
using GraphQL.Types;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Api.GraphQl.Models.Catalog;
using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog.Types;

public sealed class CategoryType : ObjectGraphType<CategoryModel>
{
    public CategoryType(IDataLoaderContextAccessor loaderAccessor)
    {
        Name = "CategoryType";
        Description = "Describes category type";

        Field(c => c.Id).Description("Category ID");
        Field(c => c.Name).Description("Category name");
        Field<CategoryType>("parent")
            .Description("Category's parent")
            .ResolveAsync(async context =>
            {
                if (!context.Source.ParentId.HasValue)
                    return null;
                var categoryDataLoader = context.RequestServices!.GetRequiredService<ICategoryDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddBatchLoader<Guid, CategoryDto>(CategoryDataLoader.GET_BY_ID, categoryDataLoader.GetCategoriesByIdsAsync);
                var foundCategory = await loader.LoadAsync(context.Source.ParentId.Value).GetResultAsync(context.CancellationToken);
                if (foundCategory == null)
                    return null;
                return new CategoryModel(foundCategory.Id, foundCategory.Name)
                {
                    ParentId = foundCategory.ParentId
                };
            });
    }
}