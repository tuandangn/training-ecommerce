using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Api.GraphQl.Models.Catalog;
using NamEcommerce.Api.GraphQl.Schemes.Catalog.Types;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog;

public sealed class CatalogQuery : ObjectGraphType
{
    public CatalogQuery(IDataLoaderContextAccessor loaderAccessor)
    {
        Name = "CatalogQuery";
        Description = "Describes catalog queries";

        #region Category Queries

        Field<ListGraphType<NonNullGraphType<CategoryType>>>("allCategories")
            .Description("Get all categories")
            .ResolveAsync(async context =>
            {
                var dataLoader = context.RequestServices!.GetRequiredService<ICategoryDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddLoader(CategoryDataLoader.GET_ALL, dataLoader.GetAllCategoriesAsync);
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
                var dataLoader = context.RequestServices!.GetRequiredService<ICategoryDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddBatchLoader<Guid, CategoryAppDto>(CategoryDataLoader.GET_BY_ID, dataLoader.GetCategoriesByIdsAsync);
                var foundUnitMeasurement = await loader.LoadAsync(context.GetArgument<Guid>("id")).GetResultAsync(context.CancellationToken);
                if (foundUnitMeasurement == null)
                    return null;
                return new CategoryModel(foundUnitMeasurement.Id, foundUnitMeasurement.Name)
                {
                    ParentId = foundUnitMeasurement.ParentId
                };
            });

        #endregion

        #region Unit Measurement Queries

        Field<ListGraphType<NonNullGraphType<UnitMeasurementType>>>("allUnitMeasurements")
            .Description("Get all unit measurements")
            .ResolveAsync(async context =>
            {
                var dataLoader = context.RequestServices!.GetRequiredService<IUnitMeasurementDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddLoader(UnitMeasurementDataLoader.GET_ALL, dataLoader.GetAllUnitMeasurementsAsync);
                var allUnitMeasurements = await loader.LoadAsync().GetResultAsync(context.CancellationToken);
                return allUnitMeasurements.Select(unitMeasurement => new Models.Catalog.UnitMeasurementModel(unitMeasurement.Id, unitMeasurement.Name)).ToList();
            });
        Field<UnitMeasurementType>("unitMeasurement")
            .Description("Get unit measurement by id")
            .Argument<NonNullGraphType<IdGraphType>>("id", "Unit measurement id")
            .ResolveAsync(async context =>
            {
                var dataLoader = context.RequestServices!.GetRequiredService<IUnitMeasurementDataLoader>();
                var loader = loaderAccessor.Context!.GetOrAddLoader(UnitMeasurementDataLoader.GET_BY_ID,
                    cancellationToken => dataLoader.GetUnitMeasurementByIdAsync(cancellationToken, context.GetArgument<Guid>("id"))
                );
                var foundUnitMeasurement = await loader.LoadAsync().GetResultAsync(context.CancellationToken);
                if (foundUnitMeasurement == null)
                    return null;
                return new CategoryModel(foundUnitMeasurement.Id, foundUnitMeasurement.Name);
            });


        #endregion
    }
}
