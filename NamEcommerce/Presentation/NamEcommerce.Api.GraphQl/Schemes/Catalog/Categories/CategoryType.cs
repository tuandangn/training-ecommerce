using GraphQL.Types;
using NamEcommerce.Api.GraphQl.Models.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog.Categories;

public sealed class CategoryType : ObjectGraphType<CategoryModel>
{
    public CategoryType()
    {
        Name = "CategoryType";
        Description = "Describes category type";

        Field(c => c.Id);
        Field(c => c.Name);
    }
}
