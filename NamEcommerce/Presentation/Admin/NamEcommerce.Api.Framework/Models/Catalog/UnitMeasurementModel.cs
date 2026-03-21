using System.Text.Json.Serialization;

namespace NamEcommerce.Admin.Contracts.Models.Catalog;

[Serializable]
public sealed class UnitMeasurementModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
