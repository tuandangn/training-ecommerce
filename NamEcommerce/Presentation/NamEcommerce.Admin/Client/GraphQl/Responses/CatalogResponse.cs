using NamEcommerce.Admin.Client.Models.Catalog;
using System.Text.Json.Serialization;

namespace NamEcommerce.Admin.Client.GraphQl.Responses;

[Serializable]
public sealed class CatalogResponse<TData>
{
    [JsonPropertyName("catalog")]
    public TData Catalog { get; set; }
}

[Serializable]
public sealed class CategoryResponseModel
{
    [JsonPropertyName("category")]
    public CategoryModel Category { get; set; }
}

[Serializable]
public sealed class CategoriesResponseModel
{
    [JsonPropertyName("categories")]
    public IEnumerable<CategoryModel> Categories { get; set; }
}
