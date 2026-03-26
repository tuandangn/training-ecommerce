using System.Text.RegularExpressions;

namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public abstract record BaseCategoryAppDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public required Guid? ParentId { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Name cannot be empty");
        return (true, string.Empty);
    }
}

[Serializable]
public sealed record CategoryAppDto(Guid Id) : BaseCategoryAppDto;

[Serializable]
public sealed record CreateCategoryAppDto : BaseCategoryAppDto;
[Serializable]
public sealed record CreateCategoryResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateCategoryAppDto(Guid Id) : BaseCategoryAppDto;
[Serializable]
public sealed record UpdateCategoryResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record DeleteCategoryAppDto(Guid Id);

[Serializable]
public sealed record DeleteCategoryResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
