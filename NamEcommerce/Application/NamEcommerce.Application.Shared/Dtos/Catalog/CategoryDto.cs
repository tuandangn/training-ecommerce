namespace NamEcommerce.Application.Shared.Dtos.Catalog;

[Serializable]
public sealed record CategoryDto
{
    public CategoryDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; set; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
}
