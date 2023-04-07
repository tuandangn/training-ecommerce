using System.Text.Json.Serialization;

namespace NamEcommerce.Admin.Client.Models.Catalog;

[Serializable]
public sealed class CategoryModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("parent")]
    public CategoryModel? Parent { get; set; }
}
