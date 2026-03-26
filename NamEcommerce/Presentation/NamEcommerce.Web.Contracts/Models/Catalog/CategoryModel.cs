namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record CategoryModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public required Guid? ParentId { get; set; }
}
