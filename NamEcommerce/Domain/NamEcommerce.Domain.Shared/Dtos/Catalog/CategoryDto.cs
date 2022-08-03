namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record CategoryDto
{
    public CategoryDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; init; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }

    public Guid? ParentId { get; set; }
    public int OnParentDisplayOrder { get; set; }
}
