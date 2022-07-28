namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed class CategoryDto
{
    public CategoryDto(int id, string name, int displayOrder)
        => (Id, Name, DisplayOrder) = (id, name, displayOrder);

    public int Id { get; set; }

    public string Name { get; set; }

    public int DisplayOrder { get; set; }
}
