namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public sealed record CategoryAppDto
{
    public CategoryAppDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; set; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
    public Guid? ParentId { get; set; }
}
