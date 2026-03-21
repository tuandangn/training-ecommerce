namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record CategoryDto
{
    public CategoryDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; init; }
    public string Name { get; init; }
    public int DisplayOrder { get; set; }

    public Guid? ParentId { get; set; }
    public int OnParentDisplayOrder { get; set; }
}

[Serializable]
public sealed record CreateCategoryDto
{
    public CreateCategoryDto(string name)
        => Name = name;

    public string Name { get; init; }
    public int DisplayOrder { get; set; }

    public static (bool valid, string? errorMessage) Validate(CreateCategoryDto dto)
    {
        if (dto is null)
            return (false, "Data cannot be null.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return (false, "Name is required.");

        return (true, null);
    }
}

[Serializable]
public sealed record UpdateCategoryDto
{
    public UpdateCategoryDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; init; }
    public string Name { get; init; }
    public int DisplayOrder { get; set; }
}
