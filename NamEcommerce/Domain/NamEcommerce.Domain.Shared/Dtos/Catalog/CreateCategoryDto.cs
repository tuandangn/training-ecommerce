namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed class CreateCategoryDto
{
    public CreateCategoryDto(string name, int displayOrder)
        => (Name, DisplayOrder) = (name, displayOrder);

    public string Name { get; set; }

    public int DisplayOrder { get; set; }
}
