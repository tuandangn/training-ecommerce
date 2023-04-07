using System.Text.Json.Serialization;

namespace NamEcommerce.Admin.Client.GraphQl.Responses;

[Serializable]
public sealed class CatalogResponse<TData>
{
    [JsonPropertyName("catalog")]
    public TData? Catalog { get; set; }
}
