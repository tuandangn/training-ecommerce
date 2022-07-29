namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record UpdateCategoryDto
{
    public UpdateCategoryDto(int id, string name)
        => (Id, Name) = (id, name);

    public int Id { get; init; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
}