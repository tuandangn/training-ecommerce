namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record CreateCategoryDto
{
    public CreateCategoryDto(string name)
        => Name = name;

    public string Name { get; init; }

    public int DisplayOrder { get; set; }
}
